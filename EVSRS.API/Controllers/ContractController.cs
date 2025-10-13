using EVSRS.BusinessObjects.DTO.ContractDto;
using EVSRS.Repositories.Infrastructure;
using EVSRS.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EVSRS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContractController : ControllerBase
    {
        private readonly IContractService _cotractService;
        public ContractController(IContractService cotractService)
        {
            _cotractService = cotractService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateContract([FromBody] ContractRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            await _cotractService.CreateContractAsync(request);
            return StatusCode(StatusCodes.Status201Created, new ResponseModel<string>(StatusCodes.Status201Created, ApiCodes.CREATED, null, "Created Contract successfully"));

        }

        [HttpGet("{orderBookingId}")]
        public async Task<IActionResult> GetContractByOrderId(string orderBookingId)
        {
            var contract = await _cotractService.GetContractByOrderIdAsync(orderBookingId);
            return Ok(new ResponseModel<ContractResponseDto>(StatusCodes.Status200OK, ApiCodes.SUCCESS, contract, "Fetched Contract successfully"));
        }

        [HttpGet("number/{contractNumber}")]
        public async Task<IActionResult> GetContractByNumber(string contractNumber)
        {
            var contract = await _cotractService.GetContractByNumberAsync(contractNumber);
            return Ok(new ResponseModel<ContractResponseDto>(StatusCodes.Status200OK, ApiCodes.SUCCESS, contract, "Fetched Contract successfully"));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteContract(string id)
        {
            await _cotractService.DeleteContractAsync(id);
            return Ok(new ResponseModel<string>(StatusCodes.Status200OK, ApiCodes.SUCCESS, null, "Deleted Contract successfully"));
        }

        [HttpGet]
        public async Task<IActionResult> GetAllContracts([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var contracts = await _cotractService.GetAllContractsAsync(pageNumber, pageSize);
            return Ok(new ResponseModel<PaginatedList<ContractResponseDto>>(StatusCodes.Status200OK, ApiCodes.SUCCESS, contracts, "Fetched Contracts successfully"));
        }

        [HttpGet("id/{id}")]
        public async Task<IActionResult> GetContractById(string id)
        {
            var contract = await _cotractService.GetContractByIdAsync(id);
            return Ok(new ResponseModel<ContractResponseDto>(StatusCodes.Status200OK, ApiCodes.SUCCESS, contract, "Fetched Contract successfully"));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateContract(string id, [FromBody] ContractRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            await _cotractService.UpdateContractAsync(id, request);
            return Ok(new ResponseModel<string>(StatusCodes.Status200OK, ApiCodes.SUCCESS, null, "Updated Contract successfully"));
        }


    }
}
