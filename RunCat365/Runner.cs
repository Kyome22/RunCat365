// Copyright 2025 Takuto Nakamura
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

using RunCat365.Properties;

namespace RunCat365
{
    enum Runner
    {
        Cat,
        Parrot,
        Horse,
        GamingCat,
        PartyParrot,
    }

    internal static class RunnerExtension
    {
        internal static bool IsColorPreserving(this Runner runner)
        {
            return runner switch
            {
                Runner.GamingCat => true,
                Runner.PartyParrot => true,
                _ => false,
            };
        }

        internal static string GetString(this Runner runner)
        {
            return runner switch
            {
                Runner.Cat => "Cat",
                Runner.Parrot => "Parrot",
                Runner.Horse => "Horse",
                Runner.GamingCat => "GamingCat",
                Runner.PartyParrot => "PartyParrot",
                _ => "",
            };
        }

        internal static string GetLocalizedString(this Runner runner)
        {
            return runner switch
            {
                Runner.Cat => Strings.Runner_Cat,
                Runner.Parrot => Strings.Runner_Parrot,
                Runner.Horse => Strings.Runner_Horse,
                Runner.GamingCat => Strings.Runner_GamingCat,
                Runner.PartyParrot => Strings.Runner_PartyParrot,
                _ => "",
            };
        }

        internal static int GetFrameNumber(this Runner runner)
        {
            return runner switch
            {
                Runner.Cat => 5,
                Runner.Parrot => 10,
                Runner.Horse => 14,
                Runner.GamingCat => 10,
                Runner.PartyParrot => 10,
                _ => 0,
            };
        }
    }
}
