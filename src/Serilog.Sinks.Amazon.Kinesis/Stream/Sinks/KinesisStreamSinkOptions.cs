﻿// Copyright 2014 Serilog Contributors
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Amazon.Kinesis;

namespace Serilog.Sinks.Amazon.Kinesis.Stream.Sinks
{
    /// <summary>
    ///     Provides KinesisSink with configurable options
    /// </summary>
    public class KinesisStreamSinkOptions : KinesisSinkOptionsBase
    {
        /// <summary>
        ///     Configures the Amazon Kinesis sink.
        /// </summary>
        /// <param name="streamName">The name of the Kinesis stream.</param>
        /// <param name="shardCount"></param>
        public KinesisStreamSinkOptions(string streamName) : base(streamName)
        {
        }

        /// <summary>
        ///     Will be appended to buffer base filenames.
        /// </summary>
        public override string BufferBaseFilenameAppend
        {
            get { return ".stream"; }
        }
    }
}