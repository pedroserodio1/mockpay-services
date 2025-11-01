using Microsoft.AspNetCore.Mvc;
using PaymentService.DTO;
using PaymentService.Services;

namespace PaymentService.Controller;
[ApiController]
[Route("/internal")] // Rota exclusiva para chamadas internas
public class PaymentsController : ControllerBase
{
    private readonly PaymentServices _paymentService;

    public PaymentsController(PaymentServices paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost("update-status")]
    public async Task<IActionResult> UpdateStatus([FromBody] UpdatePaymentStatusRequest request)
{
    try
    {
        // 🔹 Log inicial para debug
        Console.WriteLine("===============================================");
        Console.WriteLine("[UpdateStatus] Request recebida no endpoint /internal/update-status");
        if (request != null)
        {
            Console.WriteLine($"[UpdateStatus] TxId: {request.TxId}");
            Console.WriteLine($"[UpdateStatus] Action: {request.Action}");
        }
        else
        {
            Console.WriteLine("[UpdateStatus] Request é null!");
        }
        Console.WriteLine("===============================================");

        await _paymentService.UpdateStatusInternalAsync(request);
        
        return Ok(new { Message = $"Status do TxId {request.TxId} atualizado para {request.Action}." });
    }
    catch (KeyNotFoundException ex)
    {
        Console.WriteLine($"[UpdateStatus][NotFound] {ex.Message}");
        return NotFound(new { Message = ex.Message }); 
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[UpdateStatus][BadRequest] {ex.Message}");
        return BadRequest(new { Message = ex.Message }); 
    }
}
}