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

namespace RunCat365
{
    internal abstract class Cat
    {
        internal abstract ReadOnlySpan<int> ViolationIndices();
        internal abstract Cat Next();
        internal abstract string GetString();

        internal class Running : Cat
        {
            private static readonly int[] Frame0Violations = [5, 6, 7];
            private static readonly int[] Frame1Violations = [5, 6];
            private static readonly int[] Frame2Violations = [5, 6];
            private static readonly int[] Frame3Violations = [5];
            private static readonly int[] Frame4Violations = [5, 7];
            private static readonly int[] EmptyViolations = [];

            internal Frame CurrentFrame { get; }

            internal Running(Frame frame)
            {
                CurrentFrame = frame;
            }

            internal override ReadOnlySpan<int> ViolationIndices()
            {
                return CurrentFrame switch
                {
                    Frame.Frame0 => Frame0Violations,
                    Frame.Frame1 => Frame1Violations,
                    Frame.Frame2 => Frame2Violations,
                    Frame.Frame3 => Frame3Violations,
                    Frame.Frame4 => Frame4Violations,
                    _ => EmptyViolations,
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

        internal class Jumping : Cat
        {
            private static readonly int[] Frame0Violations = [5, 6, 7];
            private static readonly int[] Frame1Violations = [5, 6];
            private static readonly int[] Frame2Violations = [5, 6];
            private static readonly int[] Frame3Violations = [5, 6];
            private static readonly int[] Frame4Violations = [5, 6];
            private static readonly int[] Frame5Violations = [5];
            private static readonly int[] Frame6Violations = [];
            private static readonly int[] Frame7Violations = [];
            private static readonly int[] Frame8Violations = [];
            private static readonly int[] Frame9Violations = [7];
            private static readonly int[] EmptyViolations = [];

            internal Frame CurrentFrame { get; }

            internal Jumping(Frame frame)
            {
                CurrentFrame = frame;
            }

            internal override ReadOnlySpan<int> ViolationIndices()
            {
                return CurrentFrame switch
                {
                    Frame.Frame0 => Frame0Violations,
                    Frame.Frame1 => Frame1Violations,
                    Frame.Frame2 => Frame2Violations,
                    Frame.Frame3 => Frame3Violations,
                    Frame.Frame4 => Frame4Violations,
                    Frame.Frame5 => Frame5Violations,
                    Frame.Frame6 => Frame6Violations,
                    Frame.Frame7 => Frame7Violations,
                    Frame.Frame8 => Frame8Violations,
                    Frame.Frame9 => Frame9Violations,
                    _ => EmptyViolations,
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
}
