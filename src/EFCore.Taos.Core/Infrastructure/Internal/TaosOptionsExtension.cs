// Copyright (c)  maikebing All rights reserved.
//// Licensed under the MIT License, See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.EntityFrameworkCore.Taos.Infrastructure.Internal
{
    /// <summary>
    ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public class TaosOptionsExtension : RelationalOptionsExtension, IDbContextOptionsExtensionWithDebugInfo
    {
        private bool _enforceForeignKeys = true;
        private bool _loadSpatialite;
        private string _logFragment;

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public TaosOptionsExtension()
        {
        }

        // NB: When adding new options, make sure to update the copy ctor below.

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected TaosOptionsExtension([NotNull] TaosOptionsExtension copyFrom)
            : base(copyFrom)
        {
            _enforceForeignKeys = copyFrom._enforceForeignKeys;
            _loadSpatialite = copyFrom._loadSpatialite;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected override RelationalOptionsExtension Clone()
            => new TaosOptionsExtension(this);

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual bool EnforceForeignKeys => _enforceForeignKeys;

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual bool LoadSpatialite => _loadSpatialite;

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual TaosOptionsExtension WithEnforceForeignKeys(bool enforceForeignKeys)
        {
            var clone = (TaosOptionsExtension)Clone();

            clone._enforceForeignKeys = enforceForeignKeys;

            return clone;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual TaosOptionsExtension WithLoadSpatialite(bool loadSpatialite)
        {
            var clone = (TaosOptionsExtension)Clone();

            clone._loadSpatialite = loadSpatialite;

            return clone;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public override bool ApplyServices(IServiceCollection services)
        {
            Check.NotNull(services, nameof(services));
            services.AddEntityFrameworkTaos(this.ConnectionString);

            return true;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public virtual void PopulateDebugInfo(IDictionary<string, string> debugInfo)
        {
            debugInfo["Taos"] = "1";
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public override string LogFragment
        {
            get
            {
                if (_logFragment == null)
                {
                    var builder = new StringBuilder();

                    builder.Append(base.LogFragment);

                    if (!_enforceForeignKeys)
                    {
                        builder.Append("SuppressForeignKeyEnforcement ");
                    }

                    if (_loadSpatialite)
                    {
                        builder.Append("LoadSpatialite ");
                    }

                    _logFragment = builder.ToString();
                }

                return _logFragment;
            }
        }
    }
}
