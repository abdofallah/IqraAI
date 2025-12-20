using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraInfrastructure.Managers.Business;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using ProjectIqraFrontend.Middlewares;

namespace ProjectIqraFrontend.Controllers.App.Business
{
    public class UserBusinessRoutingsController : Controller
    {
        private readonly UserSessionValidationHelper _userSessionValidationHelper;
        private readonly BusinessManager _businessManager;

        public UserBusinessRoutingsController(UserSessionValidationHelper userSessionValidationHelper, BusinessManager businessManager)
        {
            _userSessionValidationHelper = userSessionValidationHelper;
            _businessManager = businessManager;
        }

        [HttpPost("/app/user/business/{businessId}/routes/save")]
        public async Task<FunctionReturnResult<BusinessAppRoute?>> SaveBusinessRoute(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppRoute?>();

            try
            {
                // Validation
                var userSessionAndBusinessValidationResult = await _userSessionValidationHelper.ValidateUserSessionAndGetUserAndBusinessAsync(
                    Request,
                    businessId,
                    checkUserDisabled: true,
                    checkUserBusinessesDisabled: true,
                    checkUserBusinessesEditingEnabled: true
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveBusinessRoute:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }
                var userData = userSessionAndBusinessValidationResult.Data!.userData!;
                var businessData = userSessionAndBusinessValidationResult.Data!.businessData!;

                // Business Inbound Routings Permission
                if (businessData.Permission.Routings.DisabledFullAt != null)
                {
                    return result.SetFailureResult(
                        "SaveBusinessRoute:BUSINESS_INBOUND_ROUTINGS_DISABLED_FULL",
                        $"Business does not have permission to access inbound routings{(string.IsNullOrEmpty(businessData.Permission.Routings.DisabledFullReason) ? "." : ": " + businessData.Permission.Routings.DisabledFullReason)}"
                    );
                }

                // Validate post type
                string? postType = formData["postType"].ToString();
                if (string.IsNullOrWhiteSpace(postType) || postType != "new" && postType != "edit")
                {
                    return result.SetFailureResult(
                        "SaveBusinessRoute:INVALID_POST_TYPE",
                        "Invalid post type."
                    );
                }

                // Validate existing route for edit
                formData.TryGetValue("existingRouteId", out StringValues existingRouteIdStringValue);
                string? existingRouteId = existingRouteIdStringValue.ToString();

                BusinessAppRoute? existingRouteData = null;
                if (postType == "new")
                {
                    if (businessData.Permission.Routings.DisabledAddingAt != null)
                    {
                        var message = "Business does not have permission to add new routes";
                        if (!string.IsNullOrEmpty(businessData.Permission.Routings.DisabledAddingReason))
                        {
                            message += ": " + businessData.Permission.Routings.DisabledAddingReason;
                        }

                        return result.SetFailureResult(
                            "SaveBusinessRoute:BUSINESS_INBOUND_ROUTINGS_DISABLED_ADDING",
                            message
                        );
                    }
                }
                else
                {
                    if (businessData.Permission.Routings.DisabledEditingAt != null)
                    {
                        var message = "Business does not have permission to edit routes";
                        if (!string.IsNullOrEmpty(businessData.Permission.Routings.DisabledEditingReason))
                        {
                            message += ": " + businessData.Permission.Routings.DisabledEditingReason;
                        }

                        return result.SetFailureResult(
                            "SaveBusinessRoute:BUSINESS_INBOUND_ROUTINGS_DISABLED_EDITING",
                            message
                        );
                    }

                    if (string.IsNullOrWhiteSpace(existingRouteId))
                    {
                        return result.SetFailureResult(
                            $"SaveBusinessRoute:MISSING_EXISTING_ROUTE_ID",
                            "Missing existing route id."
                        );
                    }

                    existingRouteData = await _businessManager.GetRoutesManager().GetBusinessRoute(businessId, existingRouteId);
                    if (existingRouteData == null)
                    {
                        return result.SetFailureResult(
                            $"SaveBusinessRoute:NOT_FOUND",
                            "Existing route not found."
                        );
                    }
                }

                // Process the save/update
                FunctionReturnResult<BusinessAppRoute?> updateResult = await _businessManager.GetRoutesManager().AddOrUpdateUserBusinessRoute(businessId, formData, postType, existingRouteData);
                if (!updateResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveBusinessRoute:{updateResult.Code}",
                        updateResult.Message
                    );
                }

                return result.SetSuccessResult(updateResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "SaveBusinessRoute:EXCEPTION",
                    $"Exception: {ex.Message}"
                );
            }
        }

        [HttpPost("/app/user/business/{businessId}/routes/{routeId}/delete")]
        public async Task<FunctionReturnResult> DeleteBusinessRoute(long businessId, string routeId)
        {
            var result = new FunctionReturnResult<BusinessAppRoute?>();

            try
            {
                // Validation
                var userSessionAndBusinessValidationResult = await _userSessionValidationHelper.ValidateUserSessionAndGetUserAndBusinessAsync(
                    Request,
                    businessId,
                    checkUserDisabled: true,
                    checkUserBusinessesDisabled: true,
                    checkUserBusinessesEditingEnabled: true
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveBusinessRoute:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }
                var userData = userSessionAndBusinessValidationResult.Data!.userData!;
                var businessData = userSessionAndBusinessValidationResult.Data!.businessData!;

                // Business Inbound Routings Permission
                if (businessData.Permission.Routings.DisabledFullAt != null)
                {
                    return result.SetFailureResult(
                        "SaveBusinessRoute:BUSINESS_INBOUND_ROUTINGS_DISABLED_FULL",
                        $"Business does not have permission to access inbound routings{(string.IsNullOrEmpty(businessData.Permission.Routings.DisabledFullReason) ? "." : ": " + businessData.Permission.Routings.DisabledFullReason)}"
                    );
                }
                if (businessData.Permission.Routings.DisabledDeletingAt != null)
                {
                    return result.SetFailureResult(
                        "SaveBusinessRoute:BUSINESS_INBOUND_ROUTINGS_DISABLED_DELETING",
                        $"Business does not have permission to delete inbound routings{(string.IsNullOrEmpty(businessData.Permission.Routings.DisabledDeletingReason) ? "." : ": " + businessData.Permission.Routings.DisabledDeletingReason)}"
                    );
                }

                var routeData = await _businessManager.GetRoutesManager().GetBusinessRoute(businessId, routeId);
                if (routeData == null)
                {
                    return result.SetFailureResult(
                        "SaveBusinessRoute:BUSINESS_INBOUND_ROUTINGS_ROUTE_NOT_FOUND",
                        $"Route not found."
                    );
                }

                var deleteResult = await _businessManager.GetRoutesManager().DeleteBusinessRoute(businessId, routeData);
                if (!deleteResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveBusinessRoute:{deleteResult.Code}",
                        deleteResult.Message
                    );
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "DeleteBusinessRoute:EXCEPTION",
                    $"Exception: {ex.Message}"
                );
            }
        }
    }
}
