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
            await _paymentService.UpdateStatusInternalAsync(request);
            
            return Ok(new { Message = $"Status do TxId {request.TxId} atualizado para {request.Action}." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message }); 
        }
        catch (Exception ex)
        {

            return BadRequest(new { Message = ex.Message }); 
        }
    }
}