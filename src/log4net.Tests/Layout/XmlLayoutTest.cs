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
using System.IO;
using System.Xml;

using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Repository;
using log4net.Tests.Appender;
using log4net.Util;

using NUnit.Framework;
using System.Globalization;

namespace log4net.Tests.Layout
{
	[TestFixture]
	public class XmlLayoutTest
	{
#if !NETSTANDARD1_3
		private CultureInfo _currentCulture;
		private CultureInfo _currentUICulture;

		[SetUp]
		public void SetUp()
		{
			// set correct thread culture
			_currentCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
			_currentUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
			System.Threading.Thread.CurrentThread.CurrentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
		}

		[TearDown]
		public void TearDown()
		{
			// restore previous culture
			System.Threading.Thread.CurrentThread.CurrentCulture = _currentCulture;
			System.Threading.Thread.CurrentThread.CurrentUICulture = _currentUICulture;
		}
#endif

		/// <summary>
		/// Build a basic <see cref="LoggingEventData"/> object with some default values.
		/// </summary>
		/// <returns>A useful LoggingEventData object</returns>
		private LoggingEventData CreateBaseEvent()
		{
			LoggingEventData ed = new LoggingEventData();
			ed.Domain = "Tests";
			ed.ExceptionString = "";
			ed.Identity = "TestRunner";
			ed.Level = Level.Info;
			ed.LocationInfo = new LocationInfo(GetType());
			ed.LoggerName = "TestLogger";
			ed.Message = "Test message";
			ed.ThreadName = "TestThread";
			ed.TimeStampUtc = DateTime.Today.ToUniversalTime();
			ed.UserName = "TestRunner";
			ed.Properties = new PropertiesDictionary();

			return ed;
		}

		private static string CreateEventNode(string message) 
		{
			string prefix = string.Empty;
			string ns = string.Empty;
			string dateTime = string.Empty;
#if NETSTANDARD || NETCOREAPP3_1_OR_GREATER
			prefix = "log4net:";
			ns = " xmlns:log4net=\"http://logging.apache.org/log4net/schemas/log4net-events-1.2\"";
#endif		
#if NET_2_0 || MONO_2_0 || MONO_3_5 || MONO_4_0 || NETSTANDARD || NETCOREAPP3_1_OR_GREATER
			dateTime = XmlConvert.ToString(DateTime.Today, XmlDateTimeSerializationMode.Local);
#else
			dateTime = XmlConvert.ToString(DateTime.Today);
#endif
			return String.Format("<{2}event logger=\"TestLogger\" timestamp=\"{0}\" level=\"INFO\" thread=\"TestThread\" domain=\"Tests\" identity=\"TestRunner\" username=\"TestRunner\"{3}><{2}message>{1}</{2}message></{2}event>" + Environment.NewLine,
			                    dateTime, message, prefix, ns);
		}

		private static string CreateEventNode(string key, string value)
		{
			string prefix = string.Empty;
			string ns = string.Empty;
			string dateTime = string.Empty;
#if NETSTANDARD || NETCOREAPP3_1_OR_GREATER
			prefix = "log4net:";
			ns = " xmlns:log4net=\"http://logging.apache.org/log4net/schemas/log4net-events-1.2\"";
#endif
#if NET_2_0 || MONO_2_0 || MONO_3_5 || MONO_4_0 || NETSTANDARD || NETCOREAPP3_1_OR_GREATER
			dateTime = XmlConvert.ToString(DateTime.Today, XmlDateTimeSerializationMode.Local);
#else
			dateTime = XmlConvert.ToString(DateTime.Today);
#endif

			return String.Format("<{3}event logger=\"TestLogger\" timestamp=\"{0}\" level=\"INFO\" thread=\"TestThread\" domain=\"Tests\" identity=\"TestRunner\" username=\"TestRunner\"{4}><{3}message>Test message</{3}message><{3}properties><{3}data name=\"{1}\" value=\"{2}\" /></{3}properties></{3}event>" + Environment.NewLine,
			                     dateTime, key, value, prefix, ns);
		}

		[Test]
		public void TestBasicEventLogging()
		{
			TextWriter writer = new StringWriter();
			XmlLayout layout = new XmlLayout();
			LoggingEventData evt = CreateBaseEvent();

			layout.Format(writer, new LoggingEvent(evt));
			string expected = CreateEventNode("Test message");
			Assert.AreEqual(expected, writer.ToString());
		}

		[Test]
		public void TestIllegalCharacterMasking()
		{
			TextWriter writer = new StringWriter();
			XmlLayout layout = new XmlLayout();
			LoggingEventData evt = CreateBaseEvent();

			evt.Message = "This is a masked char->\uFFFF";

			layout.Format(writer, new LoggingEvent(evt));

			string expected = CreateEventNode("This is a masked char-&gt;?");

			Assert.AreEqual(expected, writer.ToString());
		}

		[Test]
		public void TestCDATAEscaping1()
		{
			TextWriter writer = new StringWriter();
			XmlLayout layout = new XmlLayout();
			LoggingEventData evt = CreateBaseEvent();

			//The &'s trigger the use of a cdata block
			evt.Message = "&&&&&&&Escape this ]]>. End here.";

			layout.Format(writer, new LoggingEvent(evt));

			string expected = CreateEventNode("<![CDATA[&&&&&&&Escape this ]]>]]<![CDATA[>. End here.]]>");

			Assert.AreEqual(expected, writer.ToString());
		}

		[Test]
		public void TestCDATAEscaping2()
		{
			TextWriter writer = new StringWriter();
			XmlLayout layout = new XmlLayout();
			LoggingEventData evt = CreateBaseEvent();

			//The &'s trigger the use of a cdata block
			evt.Message = "&&&&&&&Escape the end ]]>";

			layout.Format(writer, new LoggingEvent(evt));

			string expected = CreateEventNode("<![CDATA[&&&&&&&Escape the end ]]>]]&gt;");

			Assert.AreEqual(expected, writer.ToString());
		}

		[Test]
		public void TestCDATAEscaping3()
		{
			TextWriter writer = new StringWriter();
			XmlLayout layout = new XmlLayout();
			LoggingEventData evt = CreateBaseEvent();

			//The &'s trigger the use of a cdata block
			evt.Message = "]]>&&&&&&&Escape the begining";

			layout.Format(writer, new LoggingEvent(evt));

			string expected = CreateEventNode("<![CDATA[]]>]]<![CDATA[>&&&&&&&Escape the begining]]>");

			Assert.AreEqual(expected, writer.ToString());
		}

		[Test]
		public void TestBase64EventLogging()
		{
			TextWriter writer = new StringWriter();
			XmlLayout layout = new XmlLayout();
			LoggingEventData evt = CreateBaseEvent();

			layout.Base64EncodeMessage = true;
			layout.Format(writer, new LoggingEvent(evt));

			string expected = CreateEventNode("VGVzdCBtZXNzYWdl");

			Assert.AreEqual(expected, writer.ToString());
		}

		[Test]
		public void TestPropertyEventLogging()
		{
			LoggingEventData evt = CreateBaseEvent();
			evt.Properties["Property1"] = "prop1";

			XmlLayout layout = new XmlLayout();
			StringAppender stringAppender = new StringAppender();
			stringAppender.Layout = layout;

			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			BasicConfigurator.Configure(rep, stringAppender);
			ILog log1 = LogManager.GetLogger(rep.Name, "TestThreadProperiesPattern");

			log1.Logger.Log(new LoggingEvent(evt));

			string expected = CreateEventNode("Property1", "prop1");

			Assert.AreEqual(expected, stringAppender.GetString());
		}

		[Test]
		public void TestBase64PropertyEventLogging()
		{
			LoggingEventData evt = CreateBaseEvent();
			evt.Properties["Property1"] = "prop1";

			XmlLayout layout = new XmlLayout();
			layout.Base64EncodeProperties = true;
			StringAppender stringAppender = new StringAppender();
			stringAppender.Layout = layout;

			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			BasicConfigurator.Configure(rep, stringAppender);
			ILog log1 = LogManager.GetLogger(rep.Name, "TestThreadProperiesPattern");

			log1.Logger.Log(new LoggingEvent(evt));

			string expected = CreateEventNode("Property1", "cHJvcDE=");

			Assert.AreEqual(expected, stringAppender.GetString());
		}

		[Test]
		public void TestPropertyCharacterEscaping()
		{
			LoggingEventData evt = CreateBaseEvent();
			evt.Properties["Property1"] = "prop1 \"quoted\"";

			XmlLayout layout = new XmlLayout();
			StringAppender stringAppender = new StringAppender();
			stringAppender.Layout = layout;

			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			BasicConfigurator.Configure(rep, stringAppender);
			ILog log1 = LogManager.GetLogger(rep.Name, "TestThreadProperiesPattern");

			log1.Logger.Log(new LoggingEvent(evt));

			string expected = CreateEventNode("Property1", "prop1 &quot;quoted&quot;");

			Assert.AreEqual(expected, stringAppender.GetString());
		}

		[Test]
		public void TestPropertyIllegalCharacterMasking()
		{
			LoggingEventData evt = CreateBaseEvent();
			evt.Properties["Property1"] = "mask this ->\uFFFF";

			XmlLayout layout = new XmlLayout();
			StringAppender stringAppender = new StringAppender();
			stringAppender.Layout = layout;

			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			BasicConfigurator.Configure(rep, stringAppender);
			ILog log1 = LogManager.GetLogger(rep.Name, "TestThreadProperiesPattern");

			log1.Logger.Log(new LoggingEvent(evt));

			string expected = CreateEventNode("Property1", "mask this -&gt;?");

			Assert.AreEqual(expected, stringAppender.GetString());
		}

		[Test]
		public void TestPropertyIllegalCharacterMaskingInName()
		{
			LoggingEventData evt = CreateBaseEvent();
			evt.Properties["Property\uFFFF"] = "mask this ->\uFFFF";

			XmlLayout layout = new XmlLayout();
			StringAppender stringAppender = new StringAppender();
			stringAppender.Layout = layout;

			ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
			BasicConfigurator.Configure(rep, stringAppender);
			ILog log1 = LogManager.GetLogger(rep.Name, "TestThreadProperiesPattern");

			log1.Logger.Log(new LoggingEvent(evt));

			string expected = CreateEventNode("Property?", "mask this -&gt;?");

			Assert.AreEqual(expected, stringAppender.GetString());
		}

#if NET_4_0 || MONO_4_0 || NETSTANDARD || NETCOREAPP3_1_OR_GREATER
        [Test]
        public void BracketsInStackTracesKeepLogWellFormed() {
            XmlLayout layout = new XmlLayout();
            StringAppender stringAppender = new StringAppender();
            stringAppender.Layout = layout;

            ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
            BasicConfigurator.Configure(rep, stringAppender);
            ILog log1 = LogManager.GetLogger(rep.Name, "TestLogger");
            Action<int> bar = foo => { 
                try {
                    throw new NullReferenceException();
                } catch (Exception ex) {
                    log1.Error(string.Format("Error {0}", foo), ex);
                }
            };
            bar(42);

            // really only asserts there is no exception
            var loggedDoc = new XmlDocument();
            loggedDoc.LoadXml(stringAppender.GetString());
        }

        [Test]
        public void BracketsInStackTracesAreEscapedProperly() {
            XmlLayout layout = new XmlLayout();
            StringAppender stringAppender = new StringAppender();
            stringAppender.Layout = layout;

            ILoggerRepository rep = LogManager.CreateRepository(Guid.NewGuid().ToString());
            BasicConfigurator.Configure(rep, stringAppender);
            ILog log1 = LogManager.GetLogger(rep.Name, "TestLogger");
            Action<int> bar = foo => {
                try {
                    throw new NullReferenceException();
                }
                catch (Exception ex) {
                    log1.Error(string.Format("Error {0}", foo), ex);
                }
            };
            bar(42);

            var log = stringAppender.GetString();
#if NETSTANDARD1_3
            var startOfExceptionText = log.IndexOf("<exception>", StringComparison.Ordinal) + 11;
            var endOfExceptionText = log.IndexOf("</exception>", StringComparison.Ordinal);
#else
#if NETSTANDARD || NETCOREAPP3_1_OR_GREATER
	        Assert.That(log, Does.Contain("<log4net:exception>"));
            var startOfExceptionText = log.IndexOf("<log4net:exception>", StringComparison.InvariantCulture) + "<log4net:exception>".Length;
            Assert.That(log, Does.Contain("</log4net:exception>"));
            var endOfExceptionText = log.IndexOf("</log4net:exception>", StringComparison.InvariantCulture);
#else
            Assert.That(log, Does.Contain("<exception>"));
            var startOfExceptionText = log.IndexOf("<exception>", StringComparison.InvariantCulture) + "<exception>".Length;
            Assert.That(log, Does.Contain("</exception>"));
            var endOfExceptionText = log.IndexOf("</exception>", StringComparison.InvariantCulture);
#endif
            var sub = log.Substring(startOfExceptionText, endOfExceptionText - startOfExceptionText);
            if (sub.StartsWith("<![CDATA["))
            {
                StringAssert.EndsWith("]]>", sub);
            }
            else
            {
                StringAssert.DoesNotContain("<", sub);
                StringAssert.DoesNotContain(">", sub);
            }
        }
#endif
#endif
	}
}