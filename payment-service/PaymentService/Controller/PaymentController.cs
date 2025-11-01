using Microsoft.AspNetCore.Mvc;
using PaymentService.DTO;
using PaymentService.Services;

namespace PaymentService.Controller;

[ApiController]
[Route("api/payments/[controller]")]
public class PaymentController : ControllerBase
{

    private readonly PaymentServices _paymentService;

    public PaymentController(PaymentServices paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost]
    public async Task<IActionResult> CreatePayment(PaymentCreateDTO payment)
    {
        var headers = Request.Headers;
        var userId = headers["X-User-ID"].FirstOrDefault();

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { Message = "User ID header is missing." });
        }
        try
        {
            var createdPayment = await _paymentService.CreatePaymentAsync(payment, userId);

            return Ok(createdPayment);
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
        catch (Exception)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return new JsonResult(new { Error = "Erro inesperado no servidor" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> getPaymentById([FromQuery] string txId)
    {
        var headers = Request.Headers;
        var userId = headers["X-User-ID"].FirstOrDefault();

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { Message = "User ID header is missing." });
        }

        var payment = await _paymentService.GetPaymentByIdAsync(txId, userId);

        return Ok(payment);
    }

    [HttpGet("simulate")]
    public async Task<IActionResult> simulatePayment([FromQuery] string txId)
    {

        var payment = await _paymentService.SimulatePayment(txId);

        if (!payment)
        {
            return BadRequest(new { Message = "Não foi possivel concluir o pagamento" });
        }

        return Ok(new { Message = "Pagamento foi pago com sucesso" });
    }


}