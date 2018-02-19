﻿using System;
using AcTools.Utils.Helpers;
using FirstFloor.ModernUI.Helpers;
using FirstFloor.ModernUI.Presentation;
using JetBrains.Annotations;

namespace AcManager.Tools.Helpers {
    public static partial class SettingsHolder {
        public sealed class PeriodEntry : Displayable {
            public TimeSpan TimeSpan { get; }

            public PeriodEntry(TimeSpan timeSpan, string displayName = null) {
                TimeSpan = timeSpan;
                DisplayName = displayName ?? (timeSpan == TimeSpan.Zero ? ToolsStrings.Common_Disabled :
                        timeSpan == TimeSpan.MaxValue ? ToolsStrings.Settings_Period_OnOpening :
                                string.Format(ToolsStrings.Settings_PeriodFormat, timeSpan.ToReadableTime()));
            }

            public PeriodEntry(string displayName) {
                TimeSpan = TimeSpan.MaxValue;
                DisplayName = displayName;
            }
        }

        public sealed class DelayEntry : Displayable {
            public TimeSpan TimeSpan { get; }

            public DelayEntry(TimeSpan timeSpan, string displayName = null) {
                TimeSpan = timeSpan;
                DisplayName = displayName ?? (timeSpan == TimeSpan.Zero ? ToolsStrings.Common_Disabled :
                        timeSpan.ToReadableTime());
            }
        }

        public sealed class SearchEngineEntry : Displayable {
            public string Value { get; }

            public SearchEngineEntry(string name, string value) {
                DisplayName = name;
                Value = value;
            }

            public string GetUri(string s, bool allowWikipedia) {
                if (Content.SearchWithWikipedia && allowWikipedia) {
                    s = @"site:wikipedia.org " + s;
                }

                return string.Format(Value, s.UriEscape(true));
            }
        }

        public enum MissingContentType {
            Car,
            Track,
            Showroom
        }

        public delegate string MissingContentUrlFunc(MissingContentType type, [NotNull] string id);

        public sealed class MissingContentSearchEntry : Displayable {
            public MissingContentUrlFunc Func { get; }

            public MissingContentSearchEntry(string name, MissingContentUrlFunc func, bool viaSearchEngine) {
                DisplayName = name;
                Func = func;
                ViaSearchEngine = viaSearchEngine;
            }

            public bool ViaSearchEngine { get; }

            public string GetUri([NotNull] string id, MissingContentType type) {
                var value = Func(type, id);
                return ViaSearchEngine || !value.Contains(@"://") ? Content.SearchEngine.GetUri(value, false) : value;
            }
        }

        public class OnlineServerEntry {
            private string _displayName;

            public string DisplayName => _displayName ?? (_displayName = (Id + 1).ToOrdinal(ToolsStrings.OrdinalizingSubject_Server));

            public int Id { get; internal set; }
        }
    }
}
