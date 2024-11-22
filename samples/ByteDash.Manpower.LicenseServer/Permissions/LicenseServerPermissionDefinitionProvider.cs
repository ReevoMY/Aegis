using ByteDash.Manpower.LicenseServer.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;
using Volo.Abp.MultiTenancy;

namespace ByteDash.Manpower.LicenseServer.Permissions;

public class LicenseServerPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(LicenseServerPermissions.GroupName);

        var booksPermission = myGroup.AddPermission(LicenseServerPermissions.Books.Default, L("Permission:Books"));
        booksPermission.AddChild(LicenseServerPermissions.Books.Create, L("Permission:Books.Create"));
        booksPermission.AddChild(LicenseServerPermissions.Books.Edit, L("Permission:Books.Edit"));
        booksPermission.AddChild(LicenseServerPermissions.Books.Delete, L("Permission:Books.Delete"));
        
        //Define your own permissions here. Example:
        //myGroup.AddPermission(LicenseServerPermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<LicenseServerResource>(name);
    }
}
