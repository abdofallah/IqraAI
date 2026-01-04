using IqraCore.Entities.Business;
using IqraCore.Entities.Business.ModulePermission.ENUM;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.WhiteLabel;
using IqraCore.Interfaces.Validation;
using IqraInfrastructure.Managers.Business;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using IqraCore.Entities.Validation;

namespace ProjectIqraFrontend.Controllers.App.Business
{
    public class UserBusinessContextController : Controller
    {
        private readonly ISessionValidationAndPermissionHelper _userSessionValidationAndPermissionHelper;
        private readonly WhiteLabelContext? _whiteLabelContext;
        private readonly BusinessManager _businessManager;

        public UserBusinessContextController(
            ISessionValidationAndPermissionHelper userSessionValidationAndPermissionHelper,
            WhiteLabelContext? whiteLabelContext,
            BusinessManager businessManager
        ) {
            _userSessionValidationAndPermissionHelper = userSessionValidationAndPermissionHelper;
            _whiteLabelContext = whiteLabelContext;
            _businessManager = businessManager;
        }

        [HttpPost("/app/user/business/{businessId}/context/branding/save")]
        public async Task<FunctionReturnResult<BusinessAppContextBranding?>> SaveBusinessContextBranding(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppContextBranding?>();

            try
            {
                // Validation
                var userSessionAndBusinessValidationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionAndBusinessWithPermissions(
                    Request: Request,
                    businessId: businessId,
                    whiteLabelContext: _whiteLabelContext,
                    // User Permission
                    checkUserDisabled: true,
                    // User Business Permission
                    checkUserBusinessesDisabled: true,
                    checkUserBusinessesEditingEnabled: true,
                    // Business Permission
                    checkBusinessIsDisabled: true,
                    checkBusinessCanBeEdited: true,
                    // Business Module Permissions,
                    ModulePermissionsToCheck: new List<ModulePermissionCheckData>()
                    {
                    new ModulePermissionCheckData()
                    {
                        ModulePath = "Context.ContextPermissions",
                        Type = BusinessModulePermissionType.Full,
                    },
                    new ModulePermissionCheckData()
                    {
                        ModulePath = "Context.ContextPermissions",
                        Type = BusinessModulePermissionType.Editing,
                    },
                    new ModulePermissionCheckData()
                    {
                        ModulePath = "Context.Branding",
                        Type = BusinessModulePermissionType.Full,
                    },
                    new ModulePermissionCheckData()
                    {
                        ModulePath = "Context.Branding",
                        Type = BusinessModulePermissionType.Editing,
                    },
                    }
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveBusinessTelephonyCampaign:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }

                FunctionReturnResult<BusinessAppContextBranding?> updateResult = await _businessManager.GetContextManager().UpdateUserBusinessContextBranding(businessId, formData);
                if (!updateResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveBusinessContextBranding:{updateResult.Code}",
                        updateResult.Message
                    );
                }

                return result.SetSuccessResult(updateResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "SaveBusinessContextBranding:EXCEPTION",
                    $"Error: {ex.Message}"
                );
            }
        }

        [HttpPost("/app/user/business/{businessId}/context/branches/save")]
        public async Task<FunctionReturnResult<BusinessAppContextBranch?>> SaveBusinessContextBranch(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppContextBranch?>();

            try
            {
                // Check New or Edit
                string? postType = formData["postType"].ToString();
                if (
                    string.IsNullOrWhiteSpace(postType) ||
                    (postType != "new" && postType != "edit")
                )
                {
                    return result.SetFailureResult(
                        "SaveBusinessContextBranch:INVALID_POST_TYPE",
                        "Invalid post type specified. Can only be 'new' or 'edit'."
                    );
                }

                // Validation
                var userSessionAndBusinessValidationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionAndBusinessWithPermissions(
                    Request: Request,
                    businessId: businessId,
                    whiteLabelContext: _whiteLabelContext,
                    // User Permission
                    checkUserDisabled: true,
                    // User Business Permission
                    checkUserBusinessesDisabled: true,
                    checkUserBusinessesEditingEnabled: true,
                    // Business Permission
                    checkBusinessIsDisabled: true,
                    checkBusinessCanBeEdited: true,
                    // Business Module Permissions,
                    ModulePermissionsToCheck: new List<ModulePermissionCheckData>()
                    {
                    new ModulePermissionCheckData()
                    {
                        ModulePath = "Context.ContextPermissions",
                        Type = BusinessModulePermissionType.Full,
                    },
                    new ModulePermissionCheckData()
                    {
                        ModulePath = "Context.ContextPermissions",
                        Type = postType == "new" ? BusinessModulePermissionType.Adding : BusinessModulePermissionType.Editing,
                    },
                    new ModulePermissionCheckData()
                    {
                        ModulePath = "Context.Branches",
                        Type = BusinessModulePermissionType.Full,
                    },
                    new ModulePermissionCheckData()
                    {
                        ModulePath = "Context.Branches",
                        Type = postType == "new" ? BusinessModulePermissionType.Adding : BusinessModulePermissionType.Editing,
                    },
                    }
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveBusinessContextBranch:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }

                string? exisitingBranchIdValue = null;
                if (postType == "edit")
                {
                    if (!formData.TryGetValue("exisitingBranchId", out StringValues exisitingToolIdStringValue))
                    {
                        return result.SetFailureResult(
                            "SaveBusinessContextBranch:MISSING_BRANCH_ID",
                            "Exisiting branch id is missing."
                        );
                    }
                    exisitingBranchIdValue = exisitingToolIdStringValue.ToString();
                    if (string.IsNullOrWhiteSpace(exisitingBranchIdValue))
                    {
                        return result.SetFailureResult(
                            "SaveBusinessContextBranch:EMPTY_BRANCH_ID",
                            "Exisiting branch id is empty."
                        );
                    }

                    bool branchExistsResult = await _businessManager.GetContextManager().CheckBusinessBranchExists(businessId, exisitingBranchIdValue);
                    if (!branchExistsResult)
                    {
                        return result.SetFailureResult(
                            $"SaveBusinessContextBranch:{result.Code}",
                            result.Message
                        );
                    }
                }

                FunctionReturnResult<BusinessAppContextBranch?> updateResult = await _businessManager.GetContextManager().AddOrUpdateUserBusinessContextBranch(businessId, formData, postType, exisitingBranchIdValue);
                if (!updateResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveBusinessContextBranch:{updateResult.Code}",
                        updateResult.Message
                    );
                }

                return result.SetSuccessResult(updateResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "SaveBusinessContextBranch:EXCEPTION",
                    $"Error: {ex.Message}"
                );
            }
        }

        [HttpPost("/app/user/business/{businessId}/context/services/save")]
        public async Task<FunctionReturnResult<BusinessAppContextService?>> SaveBusinessContextService(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppContextService?>();

            try
            {
                // Check New or Edit
                string? postType = formData["postType"].ToString();
                if (
                    string.IsNullOrWhiteSpace(postType) ||
                    (postType != "new" && postType != "edit")
                )
                {
                    return result.SetFailureResult(
                        "SaveBusinessContextService:INVALID_POST_TYPE",
                        "Invalid post type specified. Can only be 'new' or 'edit'."
                    );
                }

                // Validation
                var userSessionAndBusinessValidationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionAndBusinessWithPermissions(
                    Request: Request,
                    businessId: businessId,
                    whiteLabelContext: _whiteLabelContext,
                    // User Permission
                    checkUserDisabled: true,
                    // User Business Permission
                    checkUserBusinessesDisabled: true,
                    checkUserBusinessesEditingEnabled: true,
                    // Business Permission
                    checkBusinessIsDisabled: true,
                    checkBusinessCanBeEdited: true,
                    // Business Module Permissions,
                    ModulePermissionsToCheck: new List<ModulePermissionCheckData>()
                    {
                    new ModulePermissionCheckData()
                    {
                        ModulePath = "Context.ContextPermissions",
                        Type = BusinessModulePermissionType.Full,
                    },
                    new ModulePermissionCheckData()
                    {
                        ModulePath = "Context.ContextPermissions",
                        Type = postType == "new" ? BusinessModulePermissionType.Adding : BusinessModulePermissionType.Editing,
                    },
                    new ModulePermissionCheckData()
                    {
                        ModulePath = "Context.Branches",
                        Type = BusinessModulePermissionType.Full,
                    },
                    new ModulePermissionCheckData()
                    {
                        ModulePath = "Context.Branches",
                        Type = postType == "new" ? BusinessModulePermissionType.Adding : BusinessModulePermissionType.Editing,
                    },
                    }
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveBusinessContextService:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }

                string? exisitingServiceId = null;
                if (postType == "edit")
                {
                    if (!formData.TryGetValue("exisitingServiceId", out StringValues exisitingServiceIdStringValue))
                    {
                        return result.SetFailureResult(
                            "SaveBusinessContextService:MISSING_SERVICE_ID",
                            "Exisiting service id is missing."
                        );
                    }
                    exisitingServiceId = exisitingServiceIdStringValue.ToString();
                    if (string.IsNullOrWhiteSpace(exisitingServiceId))
                    {
                        return result.SetFailureResult(
                            "SaveBusinessContextService:EMPTY_SERVICE_ID",
                            "Exisiting service id is empty."
                        );
                    }

                    bool serviceExistsResult = await _businessManager.GetContextManager().CheckBusinessServiceExists(businessId, exisitingServiceId);
                    if (!serviceExistsResult)
                    {
                        return result.SetFailureResult(
                            "SaveBusinessContextService:SERVICE_NOT_FOUND",
                            "Service not found."
                        );
                    }
                }

                FunctionReturnResult<BusinessAppContextService?> updateResult = await _businessManager.GetContextManager().AddOrUpdateUserBusinessContextService(
                    businessId,
                    formData,
                    postType,
                    exisitingServiceId
                );

                if (!updateResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveBusinessContextService:{updateResult.Code}",
                        updateResult.Message
                    );
                }

                return result.SetSuccessResult(updateResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "SaveBusinessContextService:EXCEPTION",
                    $"Error: {ex.Message}"
                );
            }
        }

        [HttpPost("/app/user/business/{businessId}/context/products/save")]
        public async Task<FunctionReturnResult<BusinessAppContextProduct?>> SaveBusinessContextProduct(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppContextProduct?>();

            try
            {
                // Check New or Edit
                string? postType = formData["postType"].ToString();
                if (
                    string.IsNullOrWhiteSpace(postType) ||
                    (postType != "new" && postType != "edit")
                )
                {
                    return result.SetFailureResult(
                        "SaveBusinessContextProduct:INVALID_POST_TYPE",
                        "Invalid post type specified. Can only be 'new' or 'edit'."
                    );
                }

                // Validation
                var userSessionAndBusinessValidationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionAndBusinessWithPermissions(
                    Request: Request,
                    businessId: businessId,
                    whiteLabelContext: _whiteLabelContext,
                    // User Permission
                    checkUserDisabled: true,
                    // User Business Permission
                    checkUserBusinessesDisabled: true,
                    checkUserBusinessesEditingEnabled: true,
                    // Business Permission
                    checkBusinessIsDisabled: true,
                    checkBusinessCanBeEdited: true,
                    // Business Module Permissions,
                    ModulePermissionsToCheck: new List<ModulePermissionCheckData>()
                    {
                    new ModulePermissionCheckData()
                    {
                        ModulePath = "Context.ContextPermissions",
                        Type = BusinessModulePermissionType.Full,
                    },
                    new ModulePermissionCheckData()
                    {
                        ModulePath = "Context.ContextPermissions",
                        Type = postType == "new" ? BusinessModulePermissionType.Adding : BusinessModulePermissionType.Editing,
                    },
                    new ModulePermissionCheckData()
                    {
                        ModulePath = "Context.Products",
                        Type = BusinessModulePermissionType.Full,
                    },
                    new ModulePermissionCheckData()
                    {
                        ModulePath = "Context.Products",
                        Type = postType == "new" ? BusinessModulePermissionType.Adding : BusinessModulePermissionType.Editing,
                    },
                    }
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveBusinessContextProduct:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }

                string? exisitingProductId = null;
                if (postType == "edit")
                {
                    if (!formData.TryGetValue("exisitingProductId", out StringValues exisitingProductIdStringValue))
                    {
                        return result.SetFailureResult(
                            "SaveBusinessContextProduct:MISSING_PRODUCT_ID",
                            "Exisiting product id is missing."
                        );
                    }
                    exisitingProductId = exisitingProductIdStringValue.ToString();
                    if (string.IsNullOrWhiteSpace(exisitingProductId))
                    {
                        return result.SetFailureResult(
                            "SaveBusinessContextProduct:EMPTY_PRODUCT_ID",
                            "Exisiting product id is empty."
                        );
                    }

                    bool productExistsResult = await _businessManager.GetContextManager().CheckBusinessProductExists(businessId, exisitingProductId);
                    if (!productExistsResult)
                    {
                        return result.SetFailureResult(
                            "SaveBusinessContextProduct:PRODUCT_NOT_FOUND",
                            "Product not found."
                        );
                    }
                }

                FunctionReturnResult<BusinessAppContextProduct?> updateResult = await _businessManager.GetContextManager().AddOrUpdateUserBusinessContextProduct(
                    businessId,
                    formData,
                    postType,
                    exisitingProductId
                );

                if (!updateResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveBusinessContextProduct:{updateResult.Code}",
                        updateResult.Message
                    );
                }

                return result.SetSuccessResult(updateResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "SaveBusinessContextProduct:EXCEPTION",
                    $"Error: {ex.Message}"
                );
            }
        }
    }
}
