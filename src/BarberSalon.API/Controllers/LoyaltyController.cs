using BarberSalon.API.Common;
using BarberSalon.Application.Loyalty.DTOs;
using BarberSalon.Application.Loyalty.Services;
using Microsoft.AspNetCore.Mvc;

namespace BarberSalon.API.Controllers;

[ApiController]
[Route("api/v1/loyalty")]
public sealed class LoyaltyController(LoyaltyService loyaltyService) : ControllerBase
{
    [HttpGet("accounts")]
    [ProducesResponseType(typeof(ApiResponse<List<LoyaltyAccountDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAccounts(CancellationToken ct)
    {
        var accounts = await loyaltyService.GetAllAccountsAsync(ct);
        return Ok(ApiResponse<List<LoyaltyAccountDto>>.CreateSuccess(accounts, "Loyalty accounts retrieved successfully."));
    }

    [HttpPost("{customerId:guid}/redeem")]
    [HttpPost("accounts/{customerId:guid}/redeem")]
    [ProducesResponseType(typeof(ApiResponse<LoyaltyAccountDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RedeemPoints(Guid customerId, [FromBody] RedeemPointsRequest req, CancellationToken ct)
    {
        var account = await loyaltyService.RedeemPointsAsync(customerId, req.Points, ct);
        return Ok(ApiResponse<LoyaltyAccountDto>.CreateSuccess(account, "Points redeemed successfully."));
    }

    [HttpGet("{customerId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<LoyaltyAccountDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAccount(Guid customerId, CancellationToken ct)
    {
        var account = await loyaltyService.GetOrCreateAccountAsync(customerId, ct);
        return Ok(ApiResponse<LoyaltyAccountDto>.CreateSuccess(account, "Loyalty account retrieved successfully."));
    }

    [HttpGet("accounts/{customerId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<LoyaltyAccountDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAccountByPath(Guid customerId, CancellationToken ct)
    {
        var account = await loyaltyService.GetOrCreateAccountAsync(customerId, ct);
        return Ok(ApiResponse<LoyaltyAccountDto>.CreateSuccess(account, "Loyalty account retrieved successfully."));
    }

    [HttpGet("referrals")]
    [ProducesResponseType(typeof(ApiResponse<List<ReferralDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReferrals([FromQuery] Guid? customerId, CancellationToken ct)
    {
        var referrals = await loyaltyService.GetReferralsAsync(customerId, ct);
        return Ok(ApiResponse<List<ReferralDto>>.CreateSuccess(referrals, "Referrals retrieved successfully."));
    }

    [HttpGet("validate-referral")]
    [ProducesResponseType(typeof(ApiResponse<ValidateReferralResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ValidateReferral([FromQuery] string code, [FromQuery] Guid? customerId, CancellationToken ct)
    {
        var result = await loyaltyService.ValidateReferralCodeAsync(code, customerId, ct);
        return Ok(ApiResponse<ValidateReferralResponse>.CreateSuccess(result, result.Message));
    }

    [HttpPost("apply-referral")]
    [ProducesResponseType(typeof(ApiResponse<ReferralDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApplyReferral([FromBody] ApplyReferralRequest req, CancellationToken ct)
    {
        var result = await loyaltyService.ApplyReferralAsync(req, ct);
        return Ok(ApiResponse<ReferralDto>.CreateSuccess(result, "Referral applied successfully."));
    }

    [HttpGet("accounts/{customerId:guid}/transactions")]
    [ProducesResponseType(typeof(ApiResponse<List<LoyaltyTransactionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactions(Guid customerId, CancellationToken ct)
    {
        var transactions = await loyaltyService.GetTransactionsByCustomerIdAsync(customerId, ct);
        return Ok(ApiResponse<List<LoyaltyTransactionDto>>.CreateSuccess(transactions, "Loyalty transactions retrieved successfully."));
    }

    [HttpPost("accounts/{customerId:guid}/adjust-points")]
    [ProducesResponseType(typeof(ApiResponse<LoyaltyAccountDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AdjustPoints(Guid customerId, [FromBody] AdjustPointsRequest req, CancellationToken ct)
    {
        var account = await loyaltyService.AdjustPointsAsync(customerId, req.Points, req.Reason, ct);
        return Ok(ApiResponse<LoyaltyAccountDto>.CreateSuccess(account, "Loyalty points adjusted successfully."));
    }
}
