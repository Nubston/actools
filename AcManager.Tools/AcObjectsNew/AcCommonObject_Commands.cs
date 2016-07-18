﻿using System;
using System.Windows;
using System.Windows.Input;
using AcManager.Tools.Objects;
using AcManager.Tools.SemiGui;
using FirstFloor.ModernUI.Presentation;

namespace AcManager.Tools.AcObjectsNew {
    public abstract partial class AcCommonObject {
        private ICommand _viewInExplorerCommand;
        public virtual ICommand ViewInExplorerCommand => _viewInExplorerCommand ?? (_viewInExplorerCommand = new RelayCommand(o => {
            ViewInExplorer();
        }));

        private RelayCommand _copyIdCommand;

        public RelayCommand CopyIdCommand => _copyIdCommand ?? (_copyIdCommand = new RelayCommand(o => {
            switch (o as string) {
                case "name":
                    Clipboard.SetText(DisplayName);
                    break;

                case "path":
                    Clipboard.SetText(Location);
                    break;

                default:
                    Clipboard.SetText(Id);
                    break;
            }
        }));

        private ICommand _changeIdCommand;
        public virtual ICommand ChangeIdCommand => _changeIdCommand ?? (_changeIdCommand = new RelayCommand(o => {
            try {
                var newId = (o as string)?.Trim();
                if (string.IsNullOrWhiteSpace(newId)) return;
                Rename(newId);
            } catch (ToggleException ex) {
                NonfatalError.Notify(string.Format(Resources.AcObject_CannotChangeIdExt, ex.Message), Resources.AcObject_Disabling_MakeSureNoRunnedApps);
            } catch (Exception ex) {
                NonfatalError.Notify(Resources.AcObject_CannotChangeId, Resources.AcObject_Disabling_MakeSureNoRunnedApps, ex);
            }
        }, o => !string.IsNullOrWhiteSpace(o as string)));

        private ICommand _toggleCommand;
        public virtual ICommand ToggleCommand => _toggleCommand ?? (_toggleCommand = new RelayCommand(o => {
            try {
                Toggle();
            } catch (ToggleException ex) {
                NonfatalError.Notify(string.Format(Resources.AcObject_CannotToggleExt, ex.Message), Resources.AcObject_Disabling_MakeSureNoRunnedApps);
            } catch (Exception ex) {
                NonfatalError.Notify(Resources.AcObject_CannotToggle, Resources.AcObject_Disabling_MakeSureNoRunnedApps, ex);
            }
        }));

        private ICommand _deleteCommand;

        public virtual ICommand DeleteCommand => _deleteCommand ?? (_deleteCommand = new RelayCommand(o => {
            try {
                Delete();
            } catch (Exception ex) {
                NonfatalError.Notify(Resources.AcObject_CannotDelete, Resources.AcObject_Disabling_MakeSureNoRunnedApps, ex);
            }
        }));

        private ICommand _reloadCommand;
        public virtual ICommand ReloadCommand => _reloadCommand ?? (_reloadCommand = new RelayCommand(o => {
            if (o as string == @"full") {
                Manager.Reload(Id);
            } else {
                Reload();
            }
        }));

        private ICommand _saveCommand;
        public virtual ICommand SaveCommand => _saveCommand ?? (_saveCommand = new RelayCommand(o => {
            Save();
        }, o => Changed));
    }
}
