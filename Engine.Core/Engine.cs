﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Engine.Core.Context;
using Engine.Core.DataTypes;
using Engine.Core.Rules;
using LanguageExt;
using Engine.Core.Utils;

namespace Engine.Core
{
    public delegate List<IRule> RulesRepository(ConfigurationPath path);

    public static class EngineCore
    {
        internal static Option<ConfigurationValue> CalculateRule(
            List<IRule> rules,
            GetContextValue contextByIdentityType)
        {
            return rules.Map(rule => rule.GetValue(contextByIdentityType))
                    .SkipEmpty()
                    .FirstOrNone();
        }

        public static Option<ConfigurationValue> CalculateKey(HashSet<Identity> identities,
            GetLoadedContextByIdentity loadedContext,
            ConfigurationPath path,
            RulesRepository rules)
        {
            var contextRetrieverByType = ContextHelpers.GetContextRetrieverByType(loadedContext, identities);

            var flattenContext = ContextHelpers.FlattenLoadedContext(contextRetrieverByType);

            GetContextValue recCalculateKeyContext = (key) =>
            {
                if (!key.StartsWith("@@key")) return Option<string>.None;
                return CalculateKey(identities, loadedContext, new ConfigurationPath(key.Split('_')[1]), rules).Map(x => x.ToString());
            };

            var fullContext = ContextHelpers.Merge(flattenContext, recCalculateKeyContext);

            var fixedResult = Option<ConfigurationValue>.None;
            foreach (var identityType in identities.Select(x => x.Type))
            {
                fixedResult = ContextHelpers.GetFixedConfigurationContext(fullContext, identityType)(path);
                if (fixedResult.IsSome) break;
            }

            var result = fixedResult.IfNone(() => CalculateRule(rules(path), fullContext));

            return result;
        }
    }
}