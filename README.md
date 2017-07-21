# mycelo's PARSECS class library for DOTNET

Command-Line Parameters Parser

-----------------------------------------------------------------------------------------------------------

## Features

  + Short and long named verbs (*-o --option*)
  + Grouping of short-named verbs after a single dash (*-abc*)
  + Support for single/double dashes and/or slashes (*-option --option /option*)
  + Mutually-exclusive switches
  + Switchable options (*+on -off*)
  + String parameters (*-oSTRING -o:STRING -o=STRING -o STRING1 STRING2 STRING3*)
  + Loose parameters (before, after or amongst the verbs)
  + Parsing interruption with a lone "--" token
  + Nestable top-level commands, each with their own set of verbs
  + Automatic help text generation
  + Minimal boilerplate code

-----------------------------------------------------------------------------------------------------------

## Version Info

* 1.1.3

  + Added an `int`-indexed property to `ParsecsOption` class to retrieve its n<sup>th</sup> captured string.
  + Added the `Option` property to `ParsecsChoice` to retrieve the verb's `ParsecsOption` instance chosen by the user.

* 1.1.2

  + Added `ParsecsCommand` class to avoid erroneous calling of the `Parse` method on nested commands. It is now the return value of the `AddCommand` method. It has all members of `ParsecsParser` except `Parse`. In addition, it has a `Name` property that returns this command's verb.
  + Added indexed properties to get the `State` of each verb straight by either its `ShortName` or `LongName`.
  + Added `LooseParameters` property.
  + Parse will not fail if a verb captured more than the maximum number of strings. Instead, exceeding strings will show up on `LooseParameters`.
  + Arguments that must begin with *dash*, *slash* or *plus sign* but should not be interpreted as a verb can be escaped with `\`.
  + Fixed the chosen item's instance from a mutually-exclusive group being set as `Off` instead of `On`.

* 1.1.0

  + Added verbs capturing multiple strings.
  + Changed `GetStrings()` to `GetLooseParameters()`.
  + Parse will not fail if a verb don't capture a corresponding string. Instead, its `State` property will return `Undefined`.

* 1.0.1

  + Targeting **net40** instead of **net461** to broaden the library compatibility.

-----------------------------------------------------------------------------------------------------------

## Basic command-line parsing

* Instantiate the `Parsecs` class, define each verb by creating `ParsecsOption` instances with `AddOption` and `AddString` methods, then run the `Parse` method over the arguments' array. Once parsed, each `ParsecsOption` instance should be queried for the result of the corresponding capture.

    ```csharp
    static void Main(string[] args)
    {
        ParsecsParser main_parser = new ParsecsParser();
        ParsecsOption input = main_parser.AddString('i', "input", "Input file path");
        ParsecsOption output = main_parser.AddString('o', "output", "Output file path (optional)");
        ParsecsOption overwrite = main_parser.AddOption('w', "overwrite", "Overwrite existing file");
        ParsecsOption main_help = main_parser.AddOption('h', "help", "Display help text");

        if (main_parser.Parse(args))
        {
            if ((main_help.Switched) || (!input.Switched) || (!output.Switched))
            {
                // user requested the help text or didn't provide a required parameter
                Console.Write(main_parser.HelpText());
            }
            else
            {
                DoSomething(input.String, output.String, overwrite.Switched);
            }
        }
        else
        {
            // something went wrong in the parsing
        }
    }
    ```

  > + For each verb the short-name can be ommited by passing `default(char)` and the long-name can be ommited by passing `null`.
  > + Ommiting the HelpText parameter will hide the verb from the generated help text.
  > + The `bool` return value of the `Parse` method usually indicates that an unrecognized verb has been found. More advanced checks should be done by the application's own code.

## Defining *ON/OFF* switches

* Define the *ON/OFF* switches with the `AddOnOff` method, specifying their initial/default state.

  ```csharp
  ParsecsOption archive = main_parser.AddOnOff('a', "archive", ParsecsState.On, "Set the archive attribute");
  ParsecsOption readonly = main_parser.AddOnOff('r', "readonly", ParsecsState.Off, "Set the read-only attribute");
  ParsecsOption hidden = main_parser.AddOnOff('h', "hidden", ParsecsState.Off, "Set the hidden attribute");
  ```

  > + The same switch can be passed several times in the command-line. The final state will be defined by the last capture.

## Defining mutually-exclusive switches

* Create an instance of `ParsecsChoice` with the `AddChoice` method. Then add each choice item by creating instances of `ParsecsOption` with the `AddItem` method. The captured result is queried with the `Value` property of the `ParsecsChoice` instance.

  ```csharp
  ParsecsChoice encoding = main_parser.AddChoice('u', "Set encoding charset (default UTF-8)");
  ParsecsOption encoding_a = encoding.AddItem('a', "ansi", "ANSI charset");
  ParsecsOption encoding_u = encoding.AddItem('u', "utf8", "UTF-8 charset");
  ParsecsOption encoding_l = encoding.AddItem('l', "utf16le", "UTF-16 Litte Endian charset");
  ParsecsOption encoding_b = encoding.AddItem('b', "utf16be", "UTF-16 Big Endian charset");

  if (main_parser.Parse(args))
  {
      switch (encoding.Value) {  case 'a': /* (...) */ case 'b': // (...)
      /* OR */
      if (encoding_a.Switched) /* (...) */ else if (encoding_u.Switched) // (...)
      /* OR */
      if (main_parser['a']) /* (...) */ else if (main_parser["utf8"]) // (...)
  ```

  > + If the user does not provide any a choice, its default value will be returned by the `Value` property.
  > + If the user provides more than one choice of the same mutually-exclusive group, the `Value` property will return the last capture.
  > + The corresponding `ParsecsOption` class of each item can be queried for its individual `State` property.

## Working with nested commands

* Commands are needed when the application performs more than one distinct function and might need different sets of parameters for each one. Create nested commands on the `ParsecsParser` class with the `AddCommand` method. Retrieve the user's selected command by querying the `Command` property of the encompassing `ParsecsParser` instance. Each `ParsecsCommand` instance can have its own set of verbs.

  ```csharp
  ParsecsParser main_parser = new ParsecsParser();

  ParsecsCommand keygen = main_parser.AddCommand("keygen", "Generate a random encryption key");
  ParsecsOption kg_length = encrypt.AddString('l', "length", "Key length");

  ParsecsCommand encrypt = main_parser.AddCommand("encrypt", "Perform file encryption");
  ParsecsOption en_input = encrypt.AddString('i', "input", "Input file");
  ParsecsOption en_output = encrypt.AddString('o', "output", "Output file");
  ParsecsOption en_key = encrypt.AddString('k', "key", "Encryption key");
  ParsecsOption en_help = encrypt.AddOption('h', "help", "Show 'encrypt' parameters");

  if (main_parser.Parse(args))
  {
      if (main_parser.Command == encrypt) // or: main_parser.Command.Name == "encrypt"
      {
          if (en_help.Switched)
          {
              Console.Write(encrypt.HelpText());
          }
          else
          {
              DoEncrypt(en_input.String, en_output.String, en_key.String);
          }
      }
      else if (main_parser.Command == keygen) // or: main_parser.Command.Name == "keygen"
      {
          if (de_help.Switched)
          {
              Console.Write(keygen.HelpText());
          }
          else
          {
              DoKeyGen(Convert.ToInt32(kg_length.String));
          }
      }
      else
      {
          Console.WriteLine("invalid command");
      }
  }
  ```

  > + The user is expected to provide the command's verb as the first argument. Once a valid command is found, the rest of the arguments will be parsed by the command's corresponding parser. If the first argument does not match any command, the whole line will be parsed by the encompassing instance's own set of switches.
  > + If the user does not provide a valid command, the `Command` property will point to its own instance.
  > + The nested commands will be listed by the `HelpText()` of the encompassing instance.
  > + Each command can also have its own set of commands in a recursive fashion.
  > + All nested commands' verbs will be parsed recursively as needed.

-----------------------------------------------------------------------------------------------------------

## Classes, Methods and Properties

* **`ParsecsParser`** and **`ParsecsCommand`** class

| **Member** | **Syntax** | **Returns** | **Description** |
| :---: | :--- | :---: | :--- |
| **creator** | *[bool DoubleDash]* | `ParsecsParser` | Create the parser, optionally specifying if double-dashes are supported  (not available on `ParsecsCommand`) |
| `AddOption` | *char shortname, string LongName, [string HelpText]* | `ParsecsOption` | Create a simple verb |
| `AddOnOff` | *char ShortName, string LongName, ParsecsState DefaultState, [string HelpText]* | `ParsecsOption`  | Create a switch that can be turned on and off freely along the command-line |
| `AddString` | *char ShortName, string LongName, [int MinValues], [int MaxValues], [string HelpText]* | `ParsecsOption`  | Create a verb to capture a subsequent string, optionally specifying how many strings should be captured (default 1) |
| `AddChoice` | *[char DefaultValue], [string HelpText]* | `ParsecsChoice`  | Create a group of mutually-exclusive switches |
| `AddCommand` | *string Command, [string HelpText]* | `ParsecsParser`  | Create a nested command, later providing its own set of verbs if needed |
| `HelpText` | *[int LeftPadding], [bool UseSlashes]* | `string` | Generate the help text |
| `Parse` | *string[] args* | `bool` | Execute the command-line parsing and populate each verb instance, including any nested commands (not available on `ParsecsCommand`) |
| `Command` | *read-only property* | `ParsecsCommand` | If the user provided a valid command as the first argument, points to the nested command parser's instance, otherwise `null` |
| `Name` | *read-only property* | `string` | The nested command's own verb (not available on `ParsecsParser`) |
| `LooseParameters` | *read-only property* | `Enumerator<string>` | Captured strings along the command-line not related to any defined verb |
| `[char]` | *read-only property* | `bool` | The state of a switch, providing its short-name |
| `[string]` | *read-only property* | `bool` | The state of a switch, providing its long-name |

-----------------------------------------------------------------------------------------------------------

* **`ParsecsOption`** class

| **Member** | **Syntax** | **Returns** | **Description** |
| :---: | :--- | :---: | :--- |
| `State` | *read-only property* | `ParsecsState` | Final switch state found by the parser, also indicates if required string(s) were present |
| `Switched` | *read-only property* | `bool` | Equivalent to `State == ParsecsState.On` |
| `String` | *read-only property* | `string` | First/sole string captured by this verb |
| `Strings` | *read-only property* | `Enumerator<string>` | All strings captured by this verb |
| `[int]` | *read-only property* | `string` | The n<sup>th</sup> string captured by this verb |

-----------------------------------------------------------------------------------------------------------

* **`ParsecsChoice`** class

| **Member** | **Syntax** | **Returns** | **Description** |
| :---: | :--- | :---: | :--- |
| `AddItem` | *char ShortName, string LongName, [string HelpText]* | `ParsecsOption` | Create a new switch item into this mutually-exclusive group |
| `Option` | *read-only property* | `ParsecsOption` | If the user chose one of the items, returns the equivalent `ParsecsOption` instance, otherwise `null` |
| `Value` | *read-only property* | `char` | The short-name of the user's choice, or the default `char` value of the group |

-----------------------------------------------------------------------------------------------------------
