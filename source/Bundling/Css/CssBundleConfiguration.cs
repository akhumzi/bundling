﻿using System;
using System.Collections.Generic;
using System.Linq;
using Karambolo.AspNetCore.Bundling.Internal;
using Karambolo.AspNetCore.Bundling.Internal.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Karambolo.AspNetCore.Bundling.Css
{
    public static class CssBundleConfiguration
    {
        public const string BundleType = "css";

        internal class Configurer : BundleDefaultsConfigurerBase<BundleDefaultsOptions>
        {
            public Configurer(Action<BundleDefaultsOptions, IServiceProvider> action, IServiceProvider serviceProvider)
                : base(action, serviceProvider) { }

            protected override string Name => BundleType;

            protected override void SetDefaults(BundleDefaultsOptions options)
            {
                var helper = _serviceProvider.GetRequiredService<IEnumerable<IConfigurationHelper>>().First(h => h.Type == BundleType);

                options.GlobalDefaults = _serviceProvider.GetRequiredService<IOptions<BundleGlobalOptions>>().Value;
                options.Type = BundleType;
                options.ConcatenationToken = Environment.NewLine;

                options.ItemTransforms = helper.SetDefaultItemTransforms(options.GlobalDefaults.ItemTransforms);
                options.Transforms = helper.SetDefaultTransforms(options.GlobalDefaults.Transforms);

                options.ConfigurationHelper = helper;
            }
        }

        public class Helper : IConfigurationHelper
        {
            readonly BundleGlobalOptions _globalOptions;
            readonly CssRewriteUrlTransform _rewriteUrlTransform;
            readonly CssMinifyTransform _minifyTransform;

            public string Type => BundleType;

            public Helper(IOptions<BundleGlobalOptions> globalOptions, ICssMinifier minifier)
            {
                _globalOptions = globalOptions.Value;
                _rewriteUrlTransform = new CssRewriteUrlTransform();
                _minifyTransform = new CssMinifyTransform(minifier);
            }

            public virtual IReadOnlyList<IBundleItemTransform> SetDefaultItemTransforms(IReadOnlyList<IBundleItemTransform> itemTransforms)
            {
                return itemTransforms.ModifyIf(itemTransforms == null || !itemTransforms.Any(t => t is CssRewriteUrlTransform),
                    l => l.Add(_rewriteUrlTransform));
            }

            public virtual IReadOnlyList<IBundleTransform> SetDefaultTransforms(IReadOnlyList<IBundleTransform> transforms)
            {
                return _globalOptions.EnableMinification ? EnableMinification(transforms) : transforms;
            }

            public virtual IReadOnlyList<IBundleTransform> EnableMinification(IReadOnlyList<IBundleTransform> transforms)
            {
                return transforms.ModifyIf(transforms == null || !transforms.Any(t => t is CssMinifyTransform),
                    l => l.Add(_minifyTransform));
            }
        }

        public class ExtensionMapper : IExtensionMapper
        {
            readonly BundleDefaultsOptions _options;

            public ExtensionMapper(IOptionsMonitor<BundleDefaultsOptions> options)
            {
                _options = options.Get(BundleType);
            }

            public virtual IBundleConfiguration MapInput(string extension)
            {
                return ".css".Equals(extension, StringComparison.OrdinalIgnoreCase) ? _options : null;
            }

            public virtual IBundleConfiguration MapOutput(string extension)
            {
                return MapInput(extension);
            }
        }
    }
}
