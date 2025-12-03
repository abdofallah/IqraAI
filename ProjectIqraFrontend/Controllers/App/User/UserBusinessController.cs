using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.User;
using IqraCore.Entities.WhiteLabel;
using IqraCore.Models.User.Business;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.User;
using IqraInfrastructure.Repositories.Business;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using ProjectIqraFrontend.Middlewares;

namespace ProjectIqraFrontend.Controllers.App.User
{
    public class UserBusinessController : Controller
    {
        private readonly ILogger<UserBusinessController> _logger;
        private readonly UserSessionValidationHelper _userSessionValidationHelper;
        private readonly UserManager _userManager;
        private readonly BusinessManager _businessManager;
        private readonly UserWhiteLabelManager _userWhiteLabelManager;
        private readonly IMongoClient _mongoClient;
        private readonly WhiteLabelContext _whiteLabelContext;
        private readonly BusinessLogoRepository _businessLogoRepository;

        public UserBusinessController(
            ILogger<UserBusinessController> logger,
            UserSessionValidationHelper userSessionValidationHelper,
            UserManager userManager,
            BusinessManager businessManager,
            UserWhiteLabelManager userWhiteLabelManager,
            IMongoClient mongoClient,
            WhiteLabelContext whiteLabelContext,
            BusinessLogoRepository businessLogoRepository
        )
        {
            _logger = logger;
            _userSessionValidationHelper = userSessionValidationHelper;
            _userManager = userManager;
            _businessManager = businessManager;
            _userWhiteLabelManager = userWhiteLabelManager;
            _mongoClient = mongoClient;
            _whiteLabelContext = whiteLabelContext;
            _businessLogoRepository = businessLogoRepository;
        }


        [HttpGet("/app/user/businesses")]
        public async Task<FunctionReturnResult<List<GetUseBusinessFullResultMetaDataModel>?>> GetUserBusinesses()
        {
            var result = new FunctionReturnResult<List<GetUseBusinessFullResultMetaDataModel>?>();

            try
            {
                var validationResult = await _userSessionValidationHelper.ValidateUserSessionAndGetUserAsync(Request, checkUserDisabled: true);
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetUserBusinesses:{validationResult.Code}",
                        validationResult.Message
                    );
                }
                var userData = validationResult.Data!.userData!;

                UserPermission userPermission = userData.Permission;
                if (userPermission.Business.DisableBusinessesAt != null)
                {
                    return result.SetFailureResult(
                        $"GetUserBusinesses:USER_BUSINESSES_DISABLED",
                        "User does not have permission to view businesses" + (string.IsNullOrEmpty(userPermission.Business.DisableBusinessesReason) ? "" : ": " + userPermission.Business.DisableBusinessesReason)
                    );
                }

                FunctionReturnResult<List<BusinessData>?> businessesResult = await _businessManager.GetUserBusinessesByIds(userData.Businesses, userData.Email);
                if (!businessesResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetUserBusinesses:{businessesResult.Code}",
                        businessesResult.Message
                    );
                }

                var resultModel = new List<GetUseBusinessFullResultMetaDataModel>();
                foreach (var businessData in businessesResult.Data!)
                {
                    var businessMetaDataModel = new GetUseBusinessFullResultMetaDataModel(businessData);
                    if (businessData.LogoS3StorageLink != null)
                    {
                        businessMetaDataModel.LogoUrl = _businessLogoRepository.GeneratePresignedUrl(businessData.LogoS3StorageLink.ObjectName, 86400, businessData.LogoS3StorageLink.OriginRegion);
                    }

                    resultModel.Add(businessMetaDataModel);
                }

                return result.SetSuccessResult(resultModel);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetUserBusinesses:EXCEPTION",
                    $"Internal server error: {ex.Message}"
                );
            }
        }

        [HttpGet("/app/user/business/{businessId}")]
        public async Task<FunctionReturnResult<GetUserBusinessFullReturnModel?>> GetUserBusiness(long businessId)
        {
            var result = new FunctionReturnResult<GetUserBusinessFullReturnModel?>();

            try
            {
                var validationResult = await _userSessionValidationHelper.ValidateUserSessionAndGetUserAndBusinessAsync(
                    Request,
                    businessId,
                    checkUserDisabled: true,
                    checkUserBusinessesDisabled: true,
                    checkBusinessIsDisabled: true,
                    whiteLabelContext: _whiteLabelContext
                );
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetUserBusiness:{validationResult.Code}",
                        validationResult.Message
                    );
                }
                var userData = validationResult.Data!.userData!;
                var businessData = validationResult.Data!.businessData!;

                FunctionReturnResult<BusinessApp?> businessAppResult = await _businessManager.GetUserBusinessAppById(businessId, userData.Email);
                if (!businessAppResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetUserBusiness:{businessAppResult.Code}",
                        businessAppResult.Message
                    );
                }

                var businessMetaDataModel = new GetUseBusinessFullResultMetaDataModel(businessData);
                if (businessData.LogoS3StorageLink != null)
                {
                    businessMetaDataModel.LogoUrl = _businessLogoRepository.GeneratePresignedUrl(businessData.LogoS3StorageLink.ObjectName, 86400, businessData.LogoS3StorageLink.OriginRegion);
                }

                var resultData = new GetUserBusinessFullReturnModel()
                {
                    BusinessData = businessMetaDataModel,
                    BusinessApp = businessAppResult.Data!
                };

                return result.SetSuccessResult(resultData);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetUserBusiness:EXCEPTION",
                    $"Internal server error: {ex.Message}"
                );
            }
        }

        [HttpPost("/app/user/business/add")]
        public async Task<FunctionReturnResult<GetUseBusinessFullResultMetaDataModel?>> AddUserBusiness([FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<GetUseBusinessFullResultMetaDataModel?>();

            try
            {
                var validationResult = await _userSessionValidationHelper.ValidateUserSessionAndGetUserAsync(
                    Request,
                    checkUserDisabled: true
                );
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"AddUserBusiness:{validationResult.Code}",
                        validationResult.Message
                    );
                }
                var userData = validationResult.Data!.userData!;

                // Check User Business Permissions
                if (userData.Permission.Business.DisableBusinessesAt != null)
                {
                    return result.SetFailureResult(
                        "AddUserBusiness:BUSINESSES_DISABLED",
                        $"Bussinesses are disabled for the user{(string.IsNullOrWhiteSpace(userData.Permission.Business.DisableBusinessesReason) ? "" : ": " + userData.Permission.Business.DisableBusinessesReason)}"
                    );
                }
                if (userData.Permission.Business.AddBusinessDisabledAt != null)
                {
                    return result.SetFailureResult(
                        "AddUserBusiness:BUSINESSES_ADDING_DISABLED",
                        $"Bussinesses adding is disabled for the user{(string.IsNullOrWhiteSpace(userData.Permission.Business.AddBusinessDisableReason) ? "" : ": " + userData.Permission.Business.AddBusinessDisableReason)}"
                    );
                }

                using (var mongoSession = _mongoClient.StartSession())
                {
                    try
                    {
                        mongoSession.StartTransaction();

                        var newBusinessResult = await _businessManager.AddBusiness(
                            userData.Email,
                            formData,
                            mongoSession
                        );
                        if (!newBusinessResult.Success)
                        {
                            await mongoSession.AbortTransactionAsync();
                            return result.SetFailureResult(
                                "AddUserBusiness:" + newBusinessResult.Code,
                                newBusinessResult.Message
                            );
                        }
                        var newBusinessData = newBusinessResult.Data!;

                        await _userManager.AddBusinessIdToUser(userData.Email, newBusinessData.Id, mongoSession);

                        await mongoSession.CommitTransactionAsync();

                        var businessMetaDataModel = new GetUseBusinessFullResultMetaDataModel(newBusinessData);
                        if (newBusinessData.LogoS3StorageLink != null)
                        {
                            businessMetaDataModel.LogoUrl = _businessLogoRepository.GeneratePresignedUrl(newBusinessData.LogoS3StorageLink.ObjectName, 86400, newBusinessData.LogoS3StorageLink.OriginRegion);
                        }

                        return result.SetSuccessResult(businessMetaDataModel);
                    }
                    catch (Exception ex)
                    {
                        mongoSession.AbortTransaction();
                        return result.SetFailureResult(
                            "AddUserBusiness:MONGO_SESSION_EXCEPTION",
                            "Exception occured during mongo session."
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "AddUserBusiness:EXCEPTION",
                    $"Internal server error: {ex.Message}"
                );
            }
        }

        [HttpPost("/app/user/business/{businessId}/delete")]
        public async Task<FunctionReturnResult> DeleteUserBusiness(long businessId)
        {
            var result = new FunctionReturnResult();

            try
            {
                var validationResult = await _userSessionValidationHelper.ValidateUserSessionAndGetUserAndBusinessAsync(
                    Request,
                    businessId,
                    checkUserDisabled: true,
                    checkUserBusinessesDisabled: true,
                    checkUserBusinessesDeletingEnabled: true,
                    checkBusinessIsDisabled: true,
                    checkBusinessCanBeDeleted: true
                );
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetUserBusiness:{validationResult.Code}",
                        validationResult.Message
                    );
                }
                var userData = validationResult.Data!.userData!;
                var businessData = validationResult.Data!.businessData!;

                using (var mongoSession = _mongoClient.StartSession())
                {
                    try
                    {
                        mongoSession.StartTransaction();

                        var deleteBusinessResult = await _businessManager.DeleteBusiness(businessId, mongoSession);
                        if (!deleteBusinessResult.Success)
                        {
                            mongoSession.AbortTransaction();
                            return result.SetFailureResult(
                                "DeleteUserBusiness:" + deleteBusinessResult.Code,
                                deleteBusinessResult.Message
                            );
                        }

                        var removeUserBusinessResult = await _userManager.RemoveBusinessFromUser(userData.Email, businessId, mongoSession);
                        if (!removeUserBusinessResult.Success)
                        {
                            mongoSession.AbortTransaction();
                            return result.SetFailureResult(
                                "DeleteUserBusiness:" + removeUserBusinessResult.Code,
                                removeUserBusinessResult.Message
                            );
                        }

                        if (!string.IsNullOrWhiteSpace(businessData.WhiteLabelAssignedCustomerEmail))
                        {
                            var removeWhiteLabelCustomerResult = await _userWhiteLabelManager.RemoveWhiteLabelCustomerBusiness(userData.Email, businessData.WhiteLabelAssignedCustomerEmail, businessId, mongoSession);
                            if (!removeWhiteLabelCustomerResult)
                            {
                                mongoSession.AbortTransaction();
                                return result.SetFailureResult(
                                    "DeleteUserBusiness:USER_WHITELABEL_CUSTOMER_BUSINESS_REMOVAL_FAILED",
                                    "Failed to remove white label customer business."
                                );
                            }
                        }

                        var cancelOutboundCallQueuesResult = await _businessManager.CancelBusinessOutboundCallQueues(businessId, mongoSession);
                        if (!cancelOutboundCallQueuesResult.Success)
                        {
                            mongoSession.AbortTransaction();
                            return result.SetFailureResult(
                                "DeleteUserBusiness:" + cancelOutboundCallQueuesResult.Code,
                                cancelOutboundCallQueuesResult.Message
                            );
                        }

                        await mongoSession.CommitTransactionAsync();

                        try
                        {
                            // TODO kill all active conversations
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "DeleteUserBusiness:EXCEPTION: Failed to kill ongoing conversations for deleted business.");
                        }

                        return result.SetSuccessResult();
                    }
                    catch (Exception ex)
                    {
                        mongoSession.AbortTransaction();
                        return result.SetFailureResult(
                            "DeleteUserBusiness:MONGO_SESSION_EXCEPTION",
                            "Exception occured during mongo session."
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "DeleteUserBusiness:EXCEPTION",
                    $"Internal server error: {ex.Message}"
                );
            }
        }
    }
}
