using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using PaymentApi.Data;

namespace PaymentApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly AppDbContext _db;
        private static readonly decimal ChargeAmount = 1.10m;

        public PaymentController(AppDbContext db)
        {
            _db = db;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> MakePayment()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
                return Unauthorized(new { error = "Invalid token" });

            await using var tx = await _db.Database.BeginTransactionAsync();

            var user = await _db.Users.AsTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null) return NotFound(new { error = "User not found" });

            if (user.Balance < ChargeAmount)
                return BadRequest(new { error = "Insufficient balance" });

            
            user.Balance = Math.Round(user.Balance - ChargeAmount, 2, MidpointRounding.ToEven);

            var payment = new Payment
            {
                UserId = user.Id,
                Amount = ChargeAmount,
                Timestamp = DateTime.UtcNow
            };

            _db.Payments.Add(payment);

            try
            {
                await _db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                await tx.RollbackAsync();
                return Conflict(new { error = "Concurrent payment detected. Please retry." });
            }

            
            return Ok(new
            {
                message = "Payment successful",
                newBalance = user.Balance,
                paymentId = payment.Id,
                timestamp = payment.Timestamp
            });
        }
    }
}