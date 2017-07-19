using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mycelo.Parsecs
{
    public enum ParsecsState
    {
        On,
        Off,
        Undefined
    }

    public class ParsecsOption
    {
        private ParsecsParser parser;

        public ParsecsState State { get { return parser.GetState(this); } }
        public bool Switched { get { return parser.GetState(this) == ParsecsState.On; } }
        public string String { get { return parser.GetString(this); } }
        public IEnumerable<string> Strings { get { return parser.GetStrings(this); } }

        internal ParsecsOption(ParsecsParser Parser)
        {
            parser = Parser;
        }
    }

    public class ParsecsChoice
    {
        private ParsecsParser parser;
        internal string helptext;

        public char Value { get { return parser.GetGroupValue(this); } }

        internal ParsecsChoice(ParsecsParser Parser, string HelpText)
        {
            helptext = HelpText;
            parser = Parser;
        }

        public ParsecsOption AddItem(char ShortName, string LongName, string HelpText = default(string))
        {
            return parser.AddChoiceItem(ShortName, LongName, HelpText, this);
        }
    }

    public class ParsecsParser
    {
        private const char EQUAL_SIGN = '=';
        private const char EQUAL_SIGN_ALT = ':';
        private const char SINGLE_DASH = '-';
        private const char SLASH = '/';
        private const char PLUS_SIGN = '+';
        private const string DOUBLE_DASH = "--";
        private const string DOUBLE_PLUS = "++";
        private readonly bool doubledash;
        private string helptext;
        private ParsecsParser command;

        public ParsecsParser Command { get { return command; } }

        private enum OptionKind
        {
            Switch,
            SwitchGroup,
            String
        }

        private class OptionData
        {
            public OptionKind optionkind;
            public object optionobject;
            public char shortname;
            public string longname;
            public string helptext;
            public bool threestate;
            public object switchgroup;
            public int minvalues;
            public int maxvalues;
            public ParsecsState state;
            public List<string> values;

            public OptionData(OptionKind OptionKind, object OptionObject, char ShortName, string LongName, string HelpText, bool ThreeState, object SwitchGroup, ParsecsState DefaultState, int MinValues, int MaxValues)
            {
                optionkind = OptionKind;
                optionobject = OptionObject;
                shortname = ShortName;
                longname = LongName;
                helptext = HelpText;
                threestate = ThreeState;
                switchgroup = SwitchGroup;
                minvalues = MinValues;
                maxvalues = MaxValues;
                state = DefaultState;
                values = new List<string>();
            }
        }

        private Dictionary<string, ParsecsParser> Commands = new Dictionary<string, ParsecsParser>();
        private Dictionary<object, OptionData> OptionByObject = new Dictionary<object, OptionData>();
        private Dictionary<char, OptionData> OptionByShort = new Dictionary<char, OptionData>();
        private Dictionary<string, OptionData> OptionByLong = new Dictionary<string, OptionData>();
        private Dictionary<object, List<OptionData>> OptionGroups = new Dictionary<object, List<OptionData>>();
        private List<OptionData> LoneOptions = new List<OptionData>();
        private Dictionary<object, OptionData> GroupValue = new Dictionary<object, OptionData>();
        private Dictionary<object, char> GroupDefault = new Dictionary<object, char>();
        private List<string> LooseParameter = new List<string>();

        public ParsecsOption AddOption(char ShortName, string LongName, string HelpText = default(string))
        {
            return AddOption(OptionKind.Switch, ShortName, LongName, HelpText, false, null, ParsecsState.Off, 0, 0);
        }

        public ParsecsOption AddOnOff(char ShortName, string LongName, ParsecsState DefaultState, string HelpText = default(string))
        {
            return AddOption(OptionKind.Switch, ShortName, LongName, HelpText, true, null, DefaultState, 0, 0);
        }

        public ParsecsChoice AddChoice(char DefaultValue = default(char), string HelpText = default(string))
        {
            ParsecsChoice group = new ParsecsChoice(this, HelpText);
            GroupDefault.Add(group, DefaultValue);
            return group;
        }

        public ParsecsOption AddString(char ShortName, string LongName, string HelpText = default(string))
        {
            return AddOption(OptionKind.String, ShortName, LongName, HelpText, false, null, ParsecsState.Undefined, 1, Int32.MaxValue);
        }

        public ParsecsOption AddString(char ShortName, string LongName, int MinValues, string HelpText = default(string))
        {
            return AddOption(OptionKind.String, ShortName, LongName, HelpText, false, null, ParsecsState.Undefined, MinValues, Int32.MaxValue);
        }

        public ParsecsOption AddString(char ShortName, string LongName, int MinValues, int MaxValues, string HelpText = default(string))
        {
            return AddOption(OptionKind.String, ShortName, LongName, HelpText, false, null, ParsecsState.Undefined, MinValues, MaxValues);
        }

        public ParsecsParser AddCommand(string Command, string HelpText = default(string))
        {
            ParsecsParser command = new ParsecsParser(doubledash, HelpText);
            Commands.Add(Command.Trim().ToLower(), command);
            return command;
        }

        internal ParsecsOption AddChoiceItem(char shortname, string longname, string helptext, ParsecsChoice switchgroup)
        {
            return AddOption(OptionKind.SwitchGroup, shortname, longname, helptext, false, switchgroup, ParsecsState.Undefined, 0, 0);
        }

        private ParsecsOption AddOption(OptionKind optionkind, char shortname, string longname, string helptext, bool threestate, object switchgroup, ParsecsState defaultstate, int minvalues, int maxvalues)
        {
            ParsecsOption optionobject = new ParsecsOption(this);
            string longnorm = (longname ?? String.Empty).Trim().ToLower();
            OptionData optiondata = new OptionData(optionkind, optionobject, shortname, longnorm, helptext, threestate, switchgroup, defaultstate, minvalues, maxvalues);
            OptionByObject.Add(optionobject, optiondata);

            try { if (shortname != default(char)) OptionByShort.Add(shortname, optiondata); } catch { throw new ArgumentException("non-unique shortname"); }
            try { if (!String.IsNullOrWhiteSpace(longnorm)) OptionByLong.Add(longnorm, optiondata); } catch { throw new ArgumentException("non-unique longname"); }

            if (switchgroup != null)
            {
                if (!OptionGroups.ContainsKey(switchgroup)) OptionGroups.Add(switchgroup, new List<OptionData>());
                OptionGroups[switchgroup].Add(optiondata);
            }
            else
            {
                LoneOptions.Add(optiondata);
            }

            return optionobject;
        }

        private OptionData ParseShortSwitch(char sign, string body)
        {
            if ((body.Length == 1) || ((body[1] != EQUAL_SIGN) && (body[1] != EQUAL_SIGN_ALT)))
            {
                foreach (char letter in body)
                {
                    OptionData option = OptionByShort[letter];

                    switch (option.optionkind)
                    {
                        case OptionKind.Switch:
                            SetSwitch(option, sign);
                            break;

                        case OptionKind.SwitchGroup:
                            SetGroup(option);
                            break;

                        case OptionKind.String:
                            return ParseString(option, (body.Length > 1) ? body.Substring(1, body.Length - 1) : String.Empty, false);
                    }
                }
            }
            else
            {
                OptionData option = OptionByShort[body[0]];

                switch (option.optionkind)
                {
                    case OptionKind.Switch:
                        SetSwitch(option, sign);
                        break;

                    case OptionKind.SwitchGroup:
                        SetGroup(option);
                        break;

                    case OptionKind.String:
                        return ParseString(option, (body.Length > 2) ? body.Substring(2, body.Length - 2) : String.Empty, true);
                }
            }

            return null;
        }

        private OptionData ParseLongSwitch(char sign, string longname)
        {
            string[] split = longname.Split(new char[] { EQUAL_SIGN, EQUAL_SIGN_ALT }, 2);

            OptionData option = OptionByLong[split[0].ToLower()];

            switch (option.optionkind)
            {
                case OptionKind.Switch:
                    SetSwitch(option, sign);
                    break;

                case OptionKind.SwitchGroup:
                    SetGroup(option);
                    break;

                case OptionKind.String:
                    return ParseString(option, (split.Length > 1) ? split[1] : String.Empty, split.Length > 1);
            }

            return null;
        }

        private OptionData ParseString(OptionData option, string value, bool equalsign)
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                if (equalsign)
                {
                    option.state = ParsecsState.Off;
                    option.values.Clear();
                    return null;
                }
                else
                {
                    if (option.values.Count < option.minvalues)
                    {
                        return option;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            else
            {
                if (option.values.Count < option.maxvalues)
                {
                    option.values.Add(value);

                    if (option.values.Count >= option.minvalues)
                    {
                        option.state = ParsecsState.On;
                        return null;
                    }
                    else
                    {
                        return option;
                    }
                }
                else
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void SetSwitch(OptionData option, char sign)
        {
            option.state = GetState(option.threestate, sign);
        }

        private void SetGroup(OptionData option)
        {
            object switchgroup = option.switchgroup;
            option.state = ParsecsState.On;

            foreach (OptionData groupitem in OptionGroups[switchgroup])
            {
                if (groupitem != option)
                {
                    option.state = ParsecsState.Off;
                }
            }

            if (GroupValue.ContainsKey(switchgroup))
            {
                GroupValue[switchgroup] = option;
            }
            else
            {
                GroupValue.Add(switchgroup, option);
            }
        }

        private ParsecsState GetState(bool threestate, char sign)
        {
            if (threestate)
            {
                switch (sign)
                {
                    case SINGLE_DASH:
                        return ParsecsState.Off;

                    case SLASH:
                        return ParsecsState.On;

                    case PLUS_SIGN:
                        return ParsecsState.On;

                    default:
                        return ParsecsState.Undefined;
                }
            }
            else
            {
                return ParsecsState.On;
            }
        }

        private IEnumerable<Tuple<string, string, string>> HelpTextGroup(IEnumerable<OptionData> options, bool useslashes)
        {
            foreach (OptionData option in options)
            {
                string column1 = String.Empty;
                string column2 = String.Empty;
                string column3 = String.Empty;

                if (option.shortname != default(char))
                {
                    if (option.threestate)
                    {
                        column1 = $"{PLUS_SIGN}|{SINGLE_DASH}{option.shortname}";
                    }
                    else
                    {
                        if (useslashes)
                        {
                            column1 = $"{SLASH}{option.shortname}";
                        }
                        else
                        {
                            column1 = $"{SINGLE_DASH}{option.shortname}";
                        }
                    }
                }

                if (!String.IsNullOrEmpty(option.longname))
                {
                    if (useslashes)
                    {
                        column2 = $"{SLASH}{option.longname}";
                    }
                    else
                    {
                        if (doubledash)
                        {
                            column2 = $"{DOUBLE_DASH}{option.longname}";
                        }
                        else
                        {
                            column2 = $"{SINGLE_DASH}{option.longname}";
                        }
                    }
                }

                if (!String.IsNullOrWhiteSpace(option.helptext))
                {
                    column3 = option.helptext;
                }

                yield return new Tuple<string, string, string>(column1, column2, column3);
            }
        }

        public ParsecsParser()
        {
            doubledash = true;
        }

        public ParsecsParser(bool DoubleDash)
        {
            doubledash = false;
        }

        private ParsecsParser(bool DoubleDash, string HelpText)
        {
            doubledash = DoubleDash;
            helptext = HelpText;
        }

        public bool Parse(string[] args)
        {
            OptionData optwaitvalue = null;

            try
            {
                if ((args.Length > 0) && Commands.ContainsKey(args[0].ToLower()))
                {
                    command = Commands[args[0].ToLower()];
                    return command.Parse(args.Skip(1).ToArray());
                }

                foreach (string arg in args)
                {
                    if (!String.IsNullOrWhiteSpace(arg))
                    {
                        if (doubledash)
                        {
                            if (arg == DOUBLE_DASH)
                            {
                                break;
                            }
                            else if ((arg.Length >= 2) && ((arg[0] == SINGLE_DASH) || (arg[0] == PLUS_SIGN)) && (arg[1] != arg[0]))
                            {
                                optwaitvalue = ParseShortSwitch(arg[0], arg.Substring(1, arg.Length - 1));
                            }
                            else if ((arg.Length == 2) && (arg[0] == SLASH))
                            {
                                optwaitvalue = ParseShortSwitch(arg[0], arg[1].ToString());
                            }
                            else if ((arg.Length > 2) && (arg[0] == SLASH))
                            {
                                optwaitvalue = ParseLongSwitch(arg[0], arg.Substring(1, arg.Length - 1));
                            }
                            else if ((arg.Length > 2) && ((arg.Substring(0, 2) == DOUBLE_DASH) || (arg.Substring(0, 2) == DOUBLE_PLUS)))
                            {
                                optwaitvalue = ParseLongSwitch(arg[0], arg.Substring(2, arg.Length - 2));
                            }
                            else
                            {
                                if (optwaitvalue == null)
                                {
                                    LooseParameter.Add(arg);
                                }
                                else
                                {
                                    ParseString(optwaitvalue, arg, false);
                                }
                            }
                        }
                        else
                        {
                            if ((arg.Length >= 2) && (arg[1] != arg[0]) && ((arg[0] == SINGLE_DASH) || (arg[0] == PLUS_SIGN) || (arg[0] == SLASH)))
                            {
                                if (arg.Length == 2)
                                {
                                    optwaitvalue = ParseShortSwitch(arg[0], arg[1].ToString());
                                }
                                else
                                {
                                    optwaitvalue = ParseLongSwitch(arg[0], arg.Substring(1, arg.Length - 1));
                                }
                            }
                            else
                            {
                                if (optwaitvalue == null)
                                {
                                    LooseParameter.Add(arg);
                                }
                                else
                                {
                                    ParseString(optwaitvalue, arg, false);
                                }
                            }
                        }
                    }
                }

                command = this;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public string HelpText(int LeftPadding = 2, bool UseSlashes = false)
        {
            StringBuilder lines = new StringBuilder();

            int max_column1 = 0;
            int max_column2 = 0;

            Action<string, string, string> calc_max = (x, y, z) =>
            {
                if (!String.IsNullOrEmpty(z))
                {
                    int lx = String.IsNullOrWhiteSpace(x) ? 0 : (x.Length + 2);
                    int ly = String.IsNullOrWhiteSpace(y) ? 0 : (y.Length + 2);
                    if (lx > max_column1) max_column1 = lx;
                    if (ly > max_column2) max_column2 = ly;
                }
            };

            Action<StringBuilder, string, string, string> print = (builder, str1, str2, str3) =>
            {
                if (!String.IsNullOrEmpty(str3))
                {
                    builder.Append(String.Empty.PadRight(LeftPadding));

                    if (!String.IsNullOrEmpty(str1))
                    {
                        if (!String.IsNullOrEmpty(str2))
                        {
                            builder.Append($"{str1},".PadRight(max_column1));
                        }
                        else
                        {
                            builder.Append(str1.PadRight(max_column1 + max_column2));
                        }
                    }
                    else
                    {
                        builder.Append(String.Empty.PadRight(max_column1));
                    }

                    if (!String.IsNullOrEmpty(str2))
                    {
                        builder.Append(str2.PadRight(max_column2));
                    }

                    builder.Append(str3);
                    builder.AppendLine();
                }
            };

            foreach (var command in Commands.Keys)
                calc_max(null, command, Commands[command].helptext);

            foreach (var columns in HelpTextGroup(LoneOptions, UseSlashes))
                calc_max(columns.Item1, columns.Item2, columns.Item3);

            foreach (object group in OptionGroups.Keys)
                foreach (var columns in HelpTextGroup(OptionGroups[group], UseSlashes))
                    calc_max(columns.Item1, columns.Item2, columns.Item3);

            foreach (var command in Commands.Keys)
                print(lines, command, String.Empty, Commands[command].helptext);

            foreach (var columns in HelpTextGroup(LoneOptions, UseSlashes))
                print(lines, columns.Item1, columns.Item2, columns.Item3);

            foreach (object group in OptionGroups.Keys)
            {
                StringBuilder sb_group = new StringBuilder();

                foreach (var columns in HelpTextGroup(OptionGroups[group], UseSlashes))
                    print(sb_group, columns.Item1, columns.Item2, columns.Item3);

                if ((!String.IsNullOrWhiteSpace((group as ParsecsChoice).helptext)) && (sb_group.Length > 0))
                {
                    if (lines.Length > 0) lines.AppendLine();
                    lines.Append(String.Empty.PadRight(LeftPadding));
                    lines.AppendLine((group as ParsecsChoice).helptext);
                    lines.Append(sb_group);
                }
            }

            return lines.ToString();
        }

        internal ParsecsState GetState(object optionobject)
        {
            if (OptionByObject.ContainsKey(optionobject))
            {
                return OptionByObject[optionobject].state;
            }
            else
            {
                return ParsecsState.Undefined;
            }
        }

        internal string GetString(object optionobject)
        {
            if (OptionByObject.ContainsKey(optionobject))
            {
                if (OptionByObject[optionobject].values.Count > 0)
                {
                    return OptionByObject[optionobject].values[0];
                }
                else
                {
                    return String.Empty;
                }
            }
            else
            {
                return String.Empty;
            }
        }

        internal IEnumerable<string> GetStrings(object optionobject)
        {
            if (OptionByObject.ContainsKey(optionobject))
            {
                return OptionByObject[optionobject].values;
            }
            else
            {
                return new string[] { };
            }
        }

        internal char GetGroupValue(object switchgroup)
        {
            if (GroupValue.ContainsKey(switchgroup))
            {
                return GroupValue[switchgroup].shortname;
            }
            else
            {
                return GroupDefault[switchgroup];
            }
        }

        public IEnumerable<string> GetLooseParameters()
        {
            return LooseParameter;
        }
    }
}
