using System;
using BlackBerry.NativeCore.Components;
using BlackBerry.NativeCore.Debugger.Requests;
using BlackBerry.NativeCore.Model;
using BlackBerry.NativeCore.Tools;

namespace BlackBerry.NativeCore.Debugger
{
    /// <summary>
    /// Wrapper class for calls, that used to be exposed by C++ GDBWrapper project.
    /// </summary>
    public static class GdbWrapper
    {
        /// <summary>
        /// Lists all running processes from specified target device.
        /// </summary>
        public static ProcessListRequest ListProcesses(GdbRunner gdb, NdkDefinition ndk, DeviceDefinition device)
        {
            if (ndk == null)
                throw new ArgumentNullException("device");
            if (device == null)
                throw new ArgumentNullException("device");

            bool ownGdb = false;

            Targets.Connect(device, ConfigDefaults.SshPublicKeyPath, null);
            if (gdb == null)
            {
                ownGdb = true;

                // start own GDB, if any specified:
                var info = new GdbInfo(ndk, device, null, null);
                gdb = new GdbRunner(info);
                gdb.ExecuteAsync();

                // wait for GDB to startup:
                Response initialInfo;
                gdb.Wait(out initialInfo);
            }

            var selectTarget = RequestsFactory.SetTargetDevice(device);
            gdb.Send(selectTarget);
            if (!selectTarget.Wait() || selectTarget.Response == null || selectTarget.Response.Name == "error")
            {
                // ask the GDB to exit, if created internally:
                if (ownGdb)
                {
                    gdb.Send(RequestsFactory.Exit());
                }

                return null;
            }

            var listRequest = RequestsFactory.ListProcesses();
            gdb.Send(listRequest);

            // wait for response:
            bool hasResponse = listRequest.Wait();

            // ask GDB to exit, if created internally:
            if (ownGdb)
            {
                gdb.Send(RequestsFactory.Exit());
            }

            return hasResponse ? listRequest : null;
        }
    }
}
