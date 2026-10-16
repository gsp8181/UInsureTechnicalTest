using Microsoft.AspNetCore.Mvc;
using UInsureTechnicalTest.DTOs;
using UInsureTechnicalTest.Services;

namespace UInsureTechnicalTest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class PolicyController : ControllerBase
    {
        private readonly IPolicyService _policyService;

        public PolicyController(IPolicyService policyService)
        {
            _policyService = policyService;
        }

        [HttpPost]
        public async Task<IActionResult> SellPolicy([FromBody] SellPolicyRequest request)
        {
            try
            {
                var policy = await _policyService.SellPolicyAsync(request);
                return CreatedAtAction(nameof(GetPolicy), new { reference = policy.PolicyReference }, policy);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{reference}")]
        public async Task<IActionResult> GetPolicy(string reference)
        {
            var policy = await _policyService.GetPolicyAsync(reference);
            if (policy == null) return NotFound();
            return Ok(policy);
        }

        [HttpPost("{reference}/renew")]
        public async Task<IActionResult> RenewPolicy(string reference)
        {
            try
            {
                var renewedPolicy = await _policyService.RenewPolicyAsync(reference);
                return Ok(renewedPolicy);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("{reference}/cancel")]
        public async Task<IActionResult> CancelPolicy(string reference)
        {
            try
            {
                var refund = await _policyService.CancelPolicyAsync(reference);
                return Ok(refund);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        //Could add an endpoint to calculate cancellation cost without actually cancelling the policy, but spec is showing 3 actions that can take place.
    }
}