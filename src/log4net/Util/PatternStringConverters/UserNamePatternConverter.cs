#region Apache License
//
// Licensed to the Apache Software Foundation (ASF) under one or more 
// contributor license agreements. See the NOTICE file distributed with
// this work for additional information regarding copyright ownership. 
// The ASF licenses this file to you under the Apache License, Version 2.0
// (the "License"); you may not use this file except in compliance with 
// the License. You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace log4net.Util.PatternStringConverters
{
	/// <summary>
	/// Write the current threads username to the output
	/// </summary>
	/// <remarks>
	/// <para>
	/// Write the current threads username to the output writer
	/// </para>
	/// </remarks>
	/// <author>Nicko Cadell</author>
	[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
	internal sealed class UserNamePatternConverter : PatternConverter 
	{
		/// <summary>
		/// Write the current threads username to the output
		/// </summary>
		/// <param name="writer">the writer to write to</param>
		/// <param name="state">null, state is not set</param>
		/// <remarks>
		/// <para>
		/// Write the current threads username to the output <paramref name="writer"/>.
		/// </para>
		/// </remarks>
		protected override void Convert(TextWriter writer, object state) 
		{
#if (NETCF || SSCLI || NETSTANDARD1_3)
			// On compact framework there's no notion of current Windows user
			writer.Write( SystemInfo.NotAvailableText );
#else
			try
			{
				System.Security.Principal.WindowsIdentity windowsIdentity = null;
				windowsIdentity = System.Security.Principal.WindowsIdentity.GetCurrent();
				if (windowsIdentity != null && windowsIdentity.Name != null)
				{
					writer.Write( windowsIdentity.Name );
				}
			}
			catch(System.Security.SecurityException)
			{
				// This security exception will occur if the caller does not have 
				// some undefined set of SecurityPermission flags.
				LogLog.Debug(declaringType, "Security exception while trying to get current windows identity. Error Ignored.");

				writer.Write( SystemInfo.NotAvailableText );
			}
#endif
		}

	    #region Private Static Fields

	    /// <summary>
	    /// The fully qualified type of the UserNamePatternConverter class.
	    /// </summary>
	    /// <remarks>
	    /// Used by the internal logger to record the Type of the
	    /// log message.
	    /// </remarks>
	    private static readonly Type declaringType = typeof(UserNamePatternConverter);

	    #endregion Private Static Fields
	}
}
