using System;
using System.Diagnostics;
using AcManager.Tools.SemiGui;
using AcTools.Processes;
using AcTools.Utils;
using FirstFloor.ModernUI.Helpers;

namespace AcManager.Tools.GameProperties {
    public abstract class GameCommandExecutorBase : Game.AdditionalProperties, IDisposable {
        public static int OptionCommandTimeout = 3000;

        private readonly Game.StartProperties _properties;

        protected GameCommandExecutorBase(Game.StartProperties properties) {
            _properties = properties;
        }

        protected void Execute(string command) {
            if (string.IsNullOrWhiteSpace(command)) return;

            command = VariablesReplacement.Process(command, _properties, null);
            Logging.Write($"[GameCommandExecutorBase] Executing command: “{command}”");

            try {
                var proc = Process.Start(new ProcessStartInfo {
                    FileName = "cmd",
                    Arguments = $"/C \"{command}\"",
                    UseShellExecute = false,
                    WorkingDirectory = FileUtils.GetDocumentsDirectory(),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                });

                if (proc == null) {
                    throw new Exception(Resources.GameCommand_UnknownProblem);
                }

                proc.OutputDataReceived += Process_OutputDataReceived;
                proc.ErrorDataReceived += Process_ErrorDataReceived;

                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                
                if (!proc.WaitForExit(OptionCommandTimeout)) {
                    proc.Kill();
                    throw new InformativeException(Resources.GameCommand_TimeoutExceeded,
                            string.Format(Resources.GameCommand_TimeoutExceeded_Commentary, (double)OptionCommandTimeout / 1000));
                }
            } catch (Exception e) {
                NonfatalError.Notify(Resources.GameCommand_CannotExecute, e);
            }
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e) {
            if (!string.IsNullOrWhiteSpace(e.Data)) {
                Logging.Write("[GameCommandExecutorBase] Output: " + e.Data);
            }
        }

        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e) {
            if (!string.IsNullOrWhiteSpace(e.Data)) {
                Logging.Write("[GameCommandExecutorBase] Error: " + e.Data);
            }
        }

        public abstract void Dispose();
    }
}