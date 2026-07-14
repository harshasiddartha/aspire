// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.Tests.Shared;
using Aspire.Dashboard.Model;
using Bunit;
using Xunit;

namespace Aspire.Dashboard.Components.Tests.Controls;

public class AspireMenuButtonTests : DashboardTestContext
{
    [Fact]
    public void Disabled_DisablesButton()
    {
        FluentUISetupHelpers.SetupFluentUIComponents(this);
        FluentUISetupHelpers.SetupFluentAnchoredRegion(this);
        FluentUISetupHelpers.SetupFluentButton(this);
        FluentUISetupHelpers.SetupFluentMenu(this);

        var cut = RenderComponent<AspireMenuButton>(builder => builder
            .Add(component => component.MenuButtonId, "disabled-menu-button")
            .Add(component => component.Items, [new MenuButtonItem { Text = "Item" }])
            .Add(component => component.Disabled, true));

        Assert.True(cut.FindComponent<Microsoft.FluentUI.AspNetCore.Components.FluentButton>().Instance.Disabled);
    }

    [Fact]
    public void Render_OmitsHostAria_AndMarksTriggerForAccessibilityObserver()
    {
        FluentUISetupHelpers.SetupFluentUIComponents(this);
        FluentUISetupHelpers.SetupFluentAnchoredRegion(this);
        FluentUISetupHelpers.SetupFluentButton(this);
        FluentUISetupHelpers.SetupFluentMenu(this);

        var cut = Render(builder =>
        {
            builder.OpenComponent<Microsoft.FluentUI.AspNetCore.Components.FluentMenuProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<AspireMenuButton>(1);
            builder.AddAttribute(2, nameof(AspireMenuButton.MenuButtonId), "view-options-button");
            builder.AddAttribute(3, nameof(AspireMenuButton.Text), "View options");
            builder.AddAttribute(4, nameof(AspireMenuButton.Items), new List<MenuButtonItem>
            {
                new MenuButtonItem
                {
                    Text = "Show hidden resources"
                }
            });
            builder.CloseComponent();
        });

        var button = cut.Find("#view-options-button");

        // Menu-button ARIA is applied by app.js at runtime, not rendered here, so bUnit (which never
        // executes app.js) asserts the host-element contract the JS observer relies on:
        //  - The data-* marker is how the single document-level observer in app.js finds every menu
        //    trigger and then sets aria-haspopup="menu" on the inner shadow-root <button part="control">.
        Assert.True(button.HasAttribute("data-aspire-menu-trigger"));

        //  - The role-less <fluent-button> host must NOT carry these ARIA attributes: aria-expanded on a
        //    role-less element is an axe-core aria-allowed-attr violation, and giving the host
        //    role="button" would trip nested-interactive against its inner <button>. They belong on the
        //    inner control (verified in a real browser by the Playwright menu-button tests). Asserting
        //    their absence here guards against regressing back to declarative host ARIA.
        Assert.False(button.HasAttribute("aria-haspopup"));
        Assert.False(button.HasAttribute("aria-expanded"));
    }

    [Fact]
    public void OnOpening_LoadsItemsBeforeMenuIsDisplayed()
    {
        FluentUISetupHelpers.SetupFluentUIComponents(this);
        FluentUISetupHelpers.SetupFluentAnchoredRegion(this);
        FluentUISetupHelpers.SetupFluentButton(this);
        FluentUISetupHelpers.SetupFluentMenu(this);

        var items = new List<MenuButtonItem>();
        var cut = Render(builder =>
        {
            builder.OpenComponent<Microsoft.FluentUI.AspNetCore.Components.FluentMenuProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<AspireMenuButton>(1);
            builder.AddAttribute(2, nameof(AspireMenuButton.MenuButtonId), "lazy-menu-button");
            builder.AddAttribute(3, nameof(AspireMenuButton.Items), items);
            builder.AddAttribute(4, nameof(AspireMenuButton.OnOpening),
                Microsoft.AspNetCore.Components.EventCallback.Factory.Create(this, () => items.Add(new MenuButtonItem { Text = "Loaded item" })));
            builder.CloseComponent();
        });

        var button = cut.Find("#lazy-menu-button");
        Assert.DoesNotContain("disabled", button.Attributes.Select(attribute => attribute.Name));

        button.Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("true", cut.Find("#lazy-menu-button").GetAttribute("aria-expanded"));
            Assert.Equal("Loaded item", cut.Find("fluent-menu-item").TextContent.Trim());
        });
    }
}
