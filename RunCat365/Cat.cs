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

namespace RunCat365;

internal abstract class Cat
{
    internal abstract List<int> ViolationIndices();
    internal abstract Cat Next();
    internal abstract string GetString();

    internal class Running(Running.Frame frame) : Cat
    {
        internal Frame CurrentFrame { get; } = frame;

        internal override List<int> ViolationIndices()
        {
            return CurrentFrame switch
            {
                Frame.Frame0 => [5, 6, 7],
                Frame.Frame1 => [5, 6],
                Frame.Frame2 => [5, 6],
                Frame.Frame3 => [5],
                Frame.Frame4 => [5, 7],
                _ => [],
            };
        }

        internal override Cat Next()
        {
            var nextFrame = (Frame)(((int)CurrentFrame + 1) % Enum.GetValues<Frame>().Length);
            return new Running(nextFrame);
        }

        internal override string GetString()
        {
            return $"running_{(int)CurrentFrame}";
        }

        internal enum Frame
        {
            Frame0,
            Frame1,
            Frame2,
            Frame3,
            Frame4
        }
    }

    internal class Jumping(Jumping.Frame frame) : Cat
    {
        internal Frame CurrentFrame { get; } = frame;

        internal override List<int> ViolationIndices()
        {
            return CurrentFrame switch
            {
                Frame.Frame0 => [5, 6, 7],
                Frame.Frame1 => [5, 6],
                Frame.Frame2 => [5, 6],
                Frame.Frame3 => [5, 6],
                Frame.Frame4 => [5, 6],
                Frame.Frame5 => [5],
                Frame.Frame6 => [],
                Frame.Frame7 => [],
                Frame.Frame8 => [],
                Frame.Frame9 => [7],
                _ => [],
            };
        }

        internal override Cat Next()
        {
            var nextFrame = (Frame)(((int)CurrentFrame + 1) % Enum.GetValues<Frame>().Length);
            return new Jumping(nextFrame);
        }

        internal override string GetString()
        {
            return $"jumping_{(int)CurrentFrame}";
        }

        internal enum Frame
        {
            Frame0,
            Frame1,
            Frame2,
            Frame3,
            Frame4,
            Frame5,
            Frame6,
            Frame7,
            Frame8,
            Frame9
        }
    }
}
