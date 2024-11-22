using ByteDash.Manpower.LicenseServer.Permissions;
using ByteDash.Manpower.LicenseServer.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Identity.Web.Navigation;
using Volo.Abp.SettingManagement.Web.Navigation;
using Volo.Abp.UI.Navigation;
using Volo.Abp.Identity.Web.Navigation;
using Volo.Abp.TenantManagement.Web.Navigation;

namespace ByteDash.Manpower.LicenseServer.Menus;

public class LicenseServerMenuContributor : IMenuContributor
{
    public async Task ConfigureMenuAsync(MenuConfigurationContext context)
    {
        if (context.Menu.Name == StandardMenus.Main)
        {
            await ConfigureMainMenuAsync(context);
        }
    }

    private static Task ConfigureMainMenuAsync(MenuConfigurationContext context)
    {
        var l = context.GetLocalizer<LicenseServerResource>();
        context.Menu.Items.Insert(
            0,
            new ApplicationMenuItem(
                LicenseServerMenus.Home,
                l["Menu:Home"],
                "~/",
                icon: "fas fa-home",
                order: 0
            )
        );


        //Administration
        var administration = context.Menu.GetAdministration();
        administration.Order = 5;
        //Administration->Tenant Management
        administration.SetSubItemOrder(TenantManagementMenuNames.GroupName, 2);
        //Administration->Identity
        administration.SetSubItemOrder(IdentityMenuNames.GroupName, 2);

        //Administration->Settings
        administration.SetSubItemOrder(SettingManagementMenuNames.GroupName, 7);
    
        context.Menu.AddItem(
            new ApplicationMenuItem(
                "BooksStore",
                l["Menu:LicenseServer"],
                icon: "fa fa-book"
            ).AddItem(
                new ApplicationMenuItem(
                    "BooksStore.Books",
                    l["Menu:Books"],
                    url: "/Books"
                ).RequirePermissions(LicenseServerPermissions.Books.Default) 
            )
        );
        
        return Task.CompletedTask;
    }
}
