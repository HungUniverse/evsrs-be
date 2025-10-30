using EVSRS.BusinessObjects.DTO.DepotDto;
using EVSRS.BusinessObjects.DTO.TransactionDto;
using EVSRS.BusinessObjects.Entity;
using EVSRS.Repositories.Infrastructure;
using EVSRS.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EVSRS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _TransactionService;
        public TransactionController(ITransactionService TransactionService)
        {
            _TransactionService = TransactionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var transactions = await _TransactionService.GetAllTransactionsAsync(page, pageSize);
            return Ok(new ResponseModel<PaginatedList<TransactionResponseDto>>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                transactions,
                "Get transactions successfully!"
            ));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTransactionById(string id)
        {
            var transaction = await _TransactionService.GetTransactionByIdAsync(id);
            if (transaction == null)
            {
                return NotFound(new ResponseModel<string>(
                    StatusCodes.Status404NotFound,
                    ApiCodes.NOT_FOUND,
                    null,
                    "Transaction not found."
                ));
            }
            return Ok(new ResponseModel<TransactionResponseDto>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                transaction,
                "Get transaction successfully!"
            ));
        }

        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetTransactionsByOrderId(string orderId)
        {
            var transactions = await _TransactionService.GetTransactionsByOrderIdAsync(orderId);
            return Ok(new ResponseModel<List<TransactionResponseDto>>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                transactions,
                "Get transactions by order ID successfully!"
            ));
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetTransactionsByUserId(string userId)
        {
            var transactions = await _TransactionService.GetTransactionsByUserIdAsync(userId);
            return Ok(new ResponseModel<List<TransactionResponseDto>>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                transactions,
                "Get transactions by user ID successfully!"
            ));
        }

        [HttpPost]
        public async Task<IActionResult> CreateTransaction([FromBody] TransactionRequestDto transactionRequestDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            await _TransactionService.CreateTransactionAsync(transactionRequestDto);
            return Ok(new ResponseModel<string>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                null,
                "Transaction created successfully!"
            ));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTransaction(string id, [FromBody] TransactionRequestDto transactionRequestDto)
        {
            var existingTransaction = await _TransactionService.GetTransactionByIdAsync(id);
            if (existingTransaction == null)
            {
                return NotFound(new ResponseModel<string>(
                    StatusCodes.Status404NotFound,
                    ApiCodes.NOT_FOUND,
                    null,
                    "Transaction not found."
                ));
            }
            await _TransactionService.UpdateTransactionAsync(id, transactionRequestDto);
            return Ok(new ResponseModel<string>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                null,
                "Transaction updated successfully!"
            ));


        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTransaction(string id)
        {
            var existingTransaction = await _TransactionService.GetTransactionByIdAsync(id);
            if (existingTransaction == null)
            {
                return NotFound(new ResponseModel<string>(
                    StatusCodes.Status404NotFound,
                    ApiCodes.NOT_FOUND,
                    null,
                    "Transaction not found."
                ));
            }
            await _TransactionService.DeleteTransactionAsync(id);
            return Ok(new ResponseModel<string>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                null,
                "Transaction deleted successfully!"
            ));
        }

    }
}
