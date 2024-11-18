﻿using Aegis.Server.AspNetCore.Localization;
using Volo.Abp.Features;
using Volo.Abp.Localization;
using Volo.Abp.Validation.StringValues;

namespace Aegis.Server.AspNetCore.Services;

/// <summary>
/// This class is used to define features for the products managed by the license server.
/// </summary>
public class SampleFeatureDefinitionProvider : FeatureDefinitionProvider
{
    public override void Define(IFeatureDefinitionContext context)
    {
        #region Product1
        var product1 = context.AddGroup("Product1");

        var p1f1 = product1.AddFeature(
            "Product1.Feature1",
            defaultValue: "false",
            displayName: LocalizableString
                .Create<FeaturesResource>("Product1.Feature1"),
            valueType: new ToggleStringValueType()
        );

        p1f1.CreateChild(
            "Product1.Feature1.Child1",
            defaultValue: "false",
            displayName: LocalizableString
                .Create<FeaturesResource>("Product1.Feature1.Child1"),
            valueType: new ToggleStringValueType()
        );

        p1f1.CreateChild(
            "Product1.Feature1.Child2",
            defaultValue: "false",
            displayName: LocalizableString
                .Create<FeaturesResource>("Product1.Feature1.Child2"),
            valueType: new ToggleStringValueType()
        );

        var p1f2 = product1.AddFeature(
            "Product1.Feature2",
            defaultValue: "false",
            displayName: LocalizableString
                .Create<FeaturesResource>("Product1.Feature2"),
            valueType: new ToggleStringValueType()
        );

        p1f2.CreateChild(
            "Product1.Feature2.Child1",
            defaultValue: "false",
            displayName: LocalizableString
                .Create<FeaturesResource>("Product1.Feature2.Child1"),
            valueType: new ToggleStringValueType()
        );
        #endregion

        #region Product2
        var product2 = context.AddGroup("Product2");

        var p2f1 = product2.AddFeature(
            "Product2.Feature1",
            defaultValue: "false",
            displayName: LocalizableString
                .Create<FeaturesResource>("Product2.Feature1"),
            valueType: new ToggleStringValueType()
        );

        p2f1.CreateChild(
            "Product2.Feature1.Child1",
            defaultValue: "false",
            displayName: LocalizableString
                .Create<FeaturesResource>("Product2.Feature1.Child1"),
            valueType: new ToggleStringValueType()
        );

        p2f1.CreateChild(
            "Product2.Feature1.Child2",
            defaultValue: "false",
            displayName: LocalizableString
                .Create<FeaturesResource>("Product2.Feature1.Child2"),
            valueType: new ToggleStringValueType()
        );

        var p2f2 = product1.AddFeature(
            "Product2.Feature2",
            defaultValue: "false",
            displayName: LocalizableString
                .Create<FeaturesResource>("Product2.Feature2"),
            valueType: new ToggleStringValueType()
        );

        p2f2.CreateChild(
            "Product2.Feature2.Child1",
            defaultValue: "false",
            displayName: LocalizableString
                .Create<FeaturesResource>("Product2.Feature2.Child1"),
            valueType: new ToggleStringValueType()
        );
        #endregion
    }
}