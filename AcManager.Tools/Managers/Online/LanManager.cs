﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using AcManager.Tools.AcManagersNew;
using AcManager.Tools.AcObjectsNew;
using AcManager.Tools.Helpers;
using AcManager.Tools.Helpers.Api;
using FirstFloor.ModernUI.Helpers;

namespace AcManager.Tools.Managers.Online {
    public class LanManager : BaseOnlineManager {
        private static LanManager _instance;

        public static LanManager Instance => _instance ?? (_instance = new LanManager());

        protected override IEnumerable<AcPlaceholderNew> ScanInner() {
            var result = new List<ServerEntry>();

            KunosApiProvider.TryToGetLanList(i => {
                try {
                    result.Add(new ServerEntry(this, i, true));
                } catch (Exception e) {
                    Logging.Warning("Cannot create ServerEntry: " + e);
                }
            });
            
            return result;
        }

        protected override Task<IEnumerable<AcPlaceholderNew>> ScanInnerAsync() {
            ScanDefferedAsync().Forget();
            return Task.FromResult((IEnumerable<AcPlaceholderNew>)new AcPlaceholderNew[0]);
        }

        private async Task ScanDefferedAsync() {
            BackgroundLoading = true;
            Pinged = 0;

            await Task.Run(() => {
                KunosApiProvider.TryToGetLanList(async i => {
                    try {
                        var entry = new ServerEntry(this, i, true);
                        InnerWrappersList.Add(new AcItemWrapper(this, entry));
                        if (entry.Status == ServerStatus.Unloaded) {
                            await entry.Update(ServerEntry.UpdateMode.Lite); // BUG: Wait?
                        }
                        Pinged++;
                    } catch (Exception e) {
                        Logging.Warning("Cannot create ServerEntry: " + e);
                    }
                });
            });

            BackgroundLoading = false;
        }
    }
}
