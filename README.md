# PARSECS class library for DOTNET

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
  > + The `BOOL` return value of the `Parse` method usually indicates that an unrecognized verb has been found. More advanced checks should be done by the user's code.


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
  encoding.AddItem('a', "ansi", "ANSI charset");
  encoding.AddItem('u', "utf8", "UTF-8 charset");
  encoding.AddItem('l', "utf16le", "UTF-16 Litte Endian charset");
  encoding.AddItem('b', "utf16be", "UTF-16 Big Endian charset");
  ```

  > + If the user does not provide any a choice, its default value will be returned by the `Value` property.
  > + If the user provides more than one choice of the same mutually-exclusive group, the `Value` property will return the last capture.


## Working with nested commands

* Commands are needed when the application performs more than one distinct function and might need different sets of parameters for each one. Create nested commands on the `ParsecsParser` class with the `AddCommand` method. Retrieve the user's selected command by querying the `Command` property of the encompassing `ParsecsParser` instance. After parsing, it will point to the corresponding instance. Each `ParsecsParser` instance can have its own set of verbs.

  ```csharp
  ParsecsParser main_parser = new ParsecsParser();

  ParsecsParser keygen = main_parser.AddCommand("keygen", "Generate a random encryption key");
  ParsecsOption kg_length = encrypt.AddString('l', "length", "Key length");

  ParsecsParser encrypt = main_parser.AddCommand("encrypt", "Perform file encryption");
  ParsecsOption en_input = encrypt.AddString('i', "input", "Input file");
  ParsecsOption en_output = encrypt.AddString('o', "output", "Output file");
  ParsecsOption en_key = encrypt.AddString('k', "key", "Encryption key");
  ParsecsOption en_help = encrypt.AddOption('h', "help", "Show 'encrypt' parameters");

  if (main_parser.Parse(args))
  {
      if (main_parser.Command == encrypt)
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
      else if (main_parser.Command == keygen)
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
  
  > + The user is expected to provide the command as the first argument. Once a valid command is found, the rest of the arguments will be parsed by the command's corresponding parser. If the first argument does not match any command, the whole line will be parsed by the encompassing instance's own set of switches.
  > + If the user does not provide a valid command, the `Command` property will point to its own instance. 
  > + The nested commands will be listed by the `HelpText()` of the encompassing instance.
  > + All nested commands will be executed recursively as needed. Usually the `Parse` method of the nested commands should not be called.
  > + Nested commands cannot share verbs between each other.

-----------------------------------------------------------------------------------------------------------

## Classes, Methods and Properties

* **`ParsecsParser`** class

| **Member** | **Syntax** | **Returns** | **Description** |
| :---: | :--- | :---: | :--- |
| **creator** | *[bool DoubleDash]* | `ParsecsParser` | Create the parser, optionally specifying if double-dashes are supported. |
| `AddOption` | *char shortname, string LongName, [string HelpText]* | `ParsecsOption` | Create a simple verb |
| `AddOnOff` | *char ShortName, string LongName, ParsecsState DefaultState, [string HelpText]* | `ParsecsOption`  | Create a switch that can be turned on and off freely along the command-line | 
| `AddString` | *char ShortName, string LongName, [int MinValues], [int MaxValues], [string HelpText]* | `ParsecsOption`  | Create a verb to capture a subsequent string, optionally specifying how many strings should be captured (default 1) |
| `AddChoice` | *[char DefaultValue], [string HelpText]* | `ParsecsChoice`  | Create a group of mutually-exclusive switches |
| `AddCommand` | *string Command, [string HelpText]* | `ParsecsParser`  | Create a nested command, later providing its own set of verbs if needed |
| `HelpText` | *[int LeftPadding], [bool UseSlashes]* | `string` | Generate the help text |
| `Parse` | *string[] args* | `bool` | Execute the command-line parsing and populate each verb instance, including any nested commands |
| `GetLooseParameters` | *none* | `Enumerator<string>` | Captured strings along the command-line not related to any defined verb |
| `Command` | *read-only property* | `ParsecsParser` | If the user provided a valid command as the first argument, points to the nested command parser's instance |

-----------------------------------------------------------------------------------------------------------

* **`ParsecsOption`** class

| **Member** | **Syntax** | **Returns** | **Description** |
| :---: | :--- | :---: | :--- |
| `State` | *read-only property* | `ParsecsState` | Final switch state found by the parser, also indicates if required string(s) were present |
| `Switched` | *read-only property* | `bool` | Equivalent to `State == ParsecsState.On` |
| `String` | *read-only property* | `string` | First/sole string passed to the argument |
| `Strings` | *read-only property* | `Enumerator<string>` | All strings captured by the argument |

-----------------------------------------------------------------------------------------------------------

* **`ParsecsChoice`** class

| **Member** | **Syntax** | **Returns** | **Description** |
| :---: | :--- | :---: | :--- |
| `AddItem` | *char ShortName, string LongName, string HelpText (optional)* | `ParsecsOption` | Create a new switch into the mutually-exclusive group |
| `Value` | *read-only property* | `char` | The short-name of the chosen switch, or the default value of the group |

-----------------------------------------------------------------------------------------------------------
