using IqraCore.Attributes;
using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Helper.Business;
using IqraCore.Entities.WhiteLabel;
using IqraCore.Interfaces.Validation;
using IqraCore.Models.User.Business;
using IqraCore.Models.S3Storage;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.User;
using IqraInfrastructure.Repositories.Business;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace ProjectIqraFrontend.Controllers.App.User
{
    public class UserBusinessController : Controller
    {
        private readonly ILogger<UserBusinessController> _logger;
        private readonly ISessionValidationAndPermissionHelper _userSessionValidationAndPermissionHelper;
        private readonly WhiteLabelContext? _whiteLabelContext;
        private readonly UserManager _userManager;
        private readonly BusinessManager _businessManager;
        private readonly IMongoClient _mongoClient;

        public UserBusinessController(
            ILogger<UserBusinessController> logger,
            ISessionValidationAndPermissionHelper userSessionValidationAndPermissionHelper,
            WhiteLabelContext? whiteLabelContext,
            UserManager userManager,
            BusinessManager businessManager,
            IMongoClient mongoClient
        )
        {
            _logger = logger;
            _userSessionValidationAndPermissionHelper = userSessionValidationAndPermissionHelper;
            _whiteLabelContext = whiteLabelContext;
            _userManager = userManager;
            _businessManager = businessManager;
            _mongoClient = mongoClient;
        }


        [HttpGet("/app/user/businesses")]
        public async Task<FunctionReturnResult<List<GetUserBusinessFullResultMetaDataModel>?>> GetUserBusinesses()
        {
            var result = new FunctionReturnResult<List<GetUserBusinessFullResultMetaDataModel>?>();

            try
            {
                var validationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionWithPermissions(
                    Request: Request,
                    checkUserDisabled: true,
                    checkUserBusinessesDisabled: true
                );
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetUserBusinesses:{validationResult.Code}",
                        validationResult.Message
                    );
                }
                var userData = validationResult.Data!.userData!;

                FunctionReturnResult<List<BusinessData>?> businessesResult = await _businessManager.GetUserBusinessesByIds(userData.Businesses, userData.Email);
                if (!businessesResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetUserBusinesses:{businessesResult.Code}",
                        businessesResult.Message
                    );
                }

                return result.SetSuccessResult(businessesResult.Data!.Select(_businessManager.CreateBusinessMetaDataModel).ToList());
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
                var validationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionAndBusinessWithPermissions(
                    Request: Request,
                    businessId: businessId,
                    whiteLabelContext: _whiteLabelContext,
                    // User Permissions
                    checkUserDisabled: true,
                    // User Business Permissions
                    checkUserBusinessesDisabled: true,
                    // Business Permissions
                    checkBusinessIsDisabled: true
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

                var businessMetaDataModel = _businessManager.CreateBusinessMetaDataModel(businessData);

                var businessAppModel = _businessManager.CreateBusinessAppModel(businessAppResult.Data!);

                var resultData = new GetUserBusinessFullReturnModel()
                {
                    BusinessData = businessMetaDataModel,
                    BusinessApp = businessAppModel
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

        [HttpGet("/app/user/business/{businessId}/signed-file/{kind}")]
        public async Task<FunctionReturnResult<S3SignedFileModel?>> GetBusinessSignedFile(long businessId, BusinessSignedFileKindENUM kind)
        {
            var result = new FunctionReturnResult<S3SignedFileModel?>();

            try
            {
                var validationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionAndBusinessWithPermissions(
                    Request: Request,
                    businessId: businessId,
                    whiteLabelContext: _whiteLabelContext,
                    // User Permissions
                    checkUserDisabled: true,
                    // User Business Permissions
                    checkUserBusinessesDisabled: true,
                    // Business Permissions
                    checkBusinessIsDisabled: false
                );
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetBusinessSignedFile:{validationResult.Code}",
                        validationResult.Message
                    );
                }
                var businessData = validationResult.Data!.businessData!;

                var signedFileResult = _businessManager.CreateBusinessSignedFile(businessData, kind);
                if (!signedFileResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetBusinessSignedFile:{signedFileResult.Code}",
                        signedFileResult.Message
                    );
                }

                return result.SetSuccessResult(signedFileResult.Data!);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetBusinessSignedFile:EXCEPTION",
                    $"Internal server error: {ex.Message}"
                );
            }
        }

        [HttpPost("/app/user/business/add")]
        public async Task<FunctionReturnResult<GetUserBusinessFullResultMetaDataModel?>> AddUserBusiness([FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<GetUserBusinessFullResultMetaDataModel?>();

            try
            {
                var validationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionWithPermissions(
                    Request: Request,
                    checkUserDisabled: true,
                    checkUserBusinessesDisabled: true,
                    checkUserBusinessesAddingEnabled: true
                );
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"AddUserBusiness:{validationResult.Code}",
                        validationResult.Message
                    );
                }
                var userData = validationResult.Data!.userData!;

                using (var mongoSession = _mongoClient.StartSession())
                {
                    mongoSession.StartTransaction();

                    try
                    {
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

                        return result.SetSuccessResult(_businessManager.CreateBusinessMetaDataModel(newBusinessData));
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

        [OpenSourceOnly]
        [HttpPost("/app/user/business/{businessId}/delete")]
        public async Task<FunctionReturnResult> DeleteUserBusiness(long businessId)
        {
            var result = new FunctionReturnResult();

            try
            {
                var validationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionAndBusinessWithPermissions(
                    Request: Request,
                    businessId: businessId,
                    // User Permissions
                    checkUserDisabled: true,
                    // User Business Permissions
                    checkUserBusinessesDisabled: true,
                    checkUserBusinessesDeletingEnabled: true,
                    // Business Permissions
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

                // Move to the business manager
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
