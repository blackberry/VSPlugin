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

using Microsoft.Build.Utilities;

namespace BlackBerry.BuildTasks
{
    sealed class PackagerCmdBuilder : CommandLineBuilder
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public PackagerCmdBuilder()
            : base(true)
        {
        }

        /// <summary>
        /// Override of IsQuotingRequired function. Always return true.
        /// </summary>
        protected override bool IsQuotingRequired(string parameter)
        {
            return true;
        }
    }
}
