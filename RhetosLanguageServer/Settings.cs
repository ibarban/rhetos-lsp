﻿using System;
using System.Collections.Generic;
using System.Text;

namespace RhetosLanguageServer
{

    public class SettingsRoot
    {
        public LanguageServerSettings RhetosLanguageServer { get; set; }
    }

    public class LanguageServerSettings
    {
        public int MaxNumberOfProblems { get; set; } = 10;

        public LanguageServerTraceSettings Trace { get; } = new LanguageServerTraceSettings();
    }

    public class LanguageServerTraceSettings
    {
        public string Server { get; set; }
    }
}
