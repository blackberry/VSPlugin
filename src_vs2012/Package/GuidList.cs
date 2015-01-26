//* Copyright 2010-2011 Research In Motion Limited.
//*
//* Licensed under the Apache License, Version 2.0 (the "License");
//* you may not use this file except in compliance with the License.
//* You may obtain a copy of the License at
//*
//* http://www.apache.org/licenses/LICENSE-2.0
//*
//* Unless required by applicable law or agreed to in writing, software
//* distributed under the License is distributed on an "AS IS" BASIS,
//* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//* See the License for the specific language governing permissions and
//* limitations under the License.

using System;

namespace BlackBerry.Package
{
    static class GuidList
    {
        public const string guidVSNDK_PackageString = "db9f9c5f-fb27-4297-ab44-fa8774e962ca";
        public const string guidVSNDK_PackageCmdSetString = "d531fe01-f48e-443d-8ea1-1530a352525f";
        public const string guidToolWindowPersistanceString = "87346a4d-fbf2-46ff-8f59-31915e39cfb9";
        public const string guidVSNDK_PackageEditorFactoryString = "1daf1ab7-917a-4fdf-9b2f-0cd801301a70";
        public const string guidVsTemplateDesignerEditorFactoryString = "5f591054-0a88-4a07-8979-44dfb1b28fd8";

        public const string guidVSStd97String = "{5efc7975-14bc-11cf-9b2b-00aa00573819}";
        public const string guidVSStd2KString = "{1496A755-94DE-11D0-8C3F-00C04FC2AAE2}";
        public const string guidVSDebugGroup = "{C9DD4A59-47FB-11D2-83E7-00C04F9902C1}"; 

        public static readonly Guid guidVSNDK_PackageCmdSet = new Guid(guidVSNDK_PackageCmdSetString);
        public static readonly Guid guidVSNDK_PackageEditorFactory = new Guid(guidVsTemplateDesignerEditorFactoryString);

        // Guid of the new pane with output messages
        public const string guidTraceMainOutputWindowPaneString = "09963E4F-18EA-40E5-BA9E-1BC375AD68D1";
        public static readonly Guid GUID_TraceMainOutputWindowPane = new Guid(guidTraceMainOutputWindowPaneString);
        public const string guidTraceGdbOutputWindowPaneString = "796156CD-B6AD-4574-9418-936BC258D38C";
        public static readonly Guid GUID_TraceGdbOutputWindowPane = new Guid(guidTraceGdbOutputWindowPaneString);
        public const string guidTraceQConnOutputWindowPaneString = "56D1A17F-2632-49A5-B678-A18B306C07AA";
        public static readonly Guid GUID_TraceQConnOutputWindowPane = new Guid(guidTraceQConnOutputWindowPaneString);
    };
}
