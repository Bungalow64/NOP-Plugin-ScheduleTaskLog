﻿using System;
using System.Collections.Generic;
using Nop.Data.Mapping;

namespace Nop.Plugin.Admin.ScheduleTaskLog.Mapping
{
    /// <inheritdoc/>
    public partial class NameCompatibility : INameCompatibility
    {
        /// <inheritdoc/>
        public Dictionary<Type, string> TableNames => new();

        /// <inheritdoc/>
        public Dictionary<(Type, string), string> ColumnName => new();
    }
}