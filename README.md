# PARSECS class library for DOTNET

Command-Line Parameters Parser

-----------------------------------------------------------------------------------------------------------

## Features

  + Short and long named switches (*-o --option*)
  + Grouping of short-named switches after a single dash (*-abc*)
  + Support for single/double dashes and/or slashes (*-option --option /option*)
  + Mutually-exclusive switches
  + Switchable options (*+on -off*)
  + String parameters (*-oSTRING -o:STRING -o=STRING -o STRING*)
  + Loose parameters (before, after or amongst the switches)
  + Nestable top-level commands, each with their own set of parameters
  + Automatic help text generation
  
-----------------------------------------------------------------------------------------------------------

## Basic command-line parsing

* Instantiate the `Parsecs` class, define each parameter by creating `ParsecsOption` instances with `AddOption` and `AddString` methods, then run the `Parse` method over the passed command-line arguments. Once parsed, each `ParsecsOption` instance should be queried for the result of the corresponding parameters.

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
            if (main_help.Switched)
            {
                Console.Write(main_parser.HelpText());
            }
            else
            {
                if (input.State != ParsecsState.Undefined)
                {
                    DoSomething(input.String, output.String, overwrite.Switched);
                }
                else
                {
                    // user did not provide required parameter
                }
            }
        }
        else
        {
            // something went wrong in the parsing
        }
    }    
    ```
  > + For each option the short-name can be ommited by passing `default(char)` and the long-name can be ommited by passing `null`
  > + The `BOOL` return value of the `Parse` method usually indicates that either an unrecognized option has been found or an option is missing its argument. More advanced checks should be done by the user's code.


## Defining *ON/OFF* switches

* Define the *ON/OFF* switches with the `AddOnOff` method, specifying its initial/default state.

  ```csharp
  ParsecsOption archive = cls.AddOnOff('a', "archive", ParsecsState.On, "Set the archive attribute");
  ParsecsOption readonly = cls.AddOnOff('r', "readonly", ParsecsState.Off, "Set the read-only attribute");
  ParsecsOption hidden = cls.AddOnOff('h', "hidden", ParsecsState.Off, "Set the hidden attribute");
  ```

  > + The same switch can be passed several times in the command-line. The final state will be defined by the last instance.


## Defining mutually-exclusive switches

* Create an instance of `ParsecsChoice` with the `AddChoice` method. Then add each choice item by creating instances of `ParsecsOption` with the `AddItem` method. The chosen option can be queried with the `Value` property of the `ParsecsChoice` instance.

  ```csharp
  ParsecsChoice encoding = cls.AddChoice('u', "Set encoding charset (default UTF-8)");
  encoding.AddItem('a', "ansi", "ANSI charset");
  encoding.AddItem('u', "utf8", "UTF-8 charset");
  encoding.AddItem('l', "utf16le", "UTF-16 Litte Endian charset");
  encoding.AddItem('b', "utf16be", "UTF-16 Big Endian charset");
  ```

  > + If the user does not provide any of the choices, the default value will be returned by the `Value` property.
  > + If the user provides more than one choice in the same command-line, the `Value` property will return the last one.


## Working with nested commands

* Create nested instances of the `ParsecsParser` class with the `AddCommand` method. Retrieve the user's selected command by querying the `Command` property of the encompassing `ParsecsParser` instance. Then test its value against each nested instance. Each `ParsecsParser` instance might have its own set of switches.

  ```csharp
  ParsecsParser main_parser = new ParsecsParser();

  ParsecsParser encrypt = main_parser.AddCommand("encrypt", "Perform file encryption");
  ParsecsOption en_input = encrypt.AddString('i', "input", "Input file");
  ParsecsOption en_output = encrypt.AddString('o', "output", "Output file");
  ParsecsOption en_help = encrypt.AddOption('h', "help", "Show 'encrypt' parameters");

  ParsecsParser decrypt = main_parser.AddCommand("decrypt", "perform file decryption");
  ParsecsOption de_input = decrypt.AddString('i', "input", "input file");
  ParsecsOption de_output = decrypt.AddString('o', "output", "output file");
  ParsecsOption de_help = decrypt.AddOption('h', "help", "Show 'decrypt' parameters");

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
              DoEncrypt(en_input.String, en_output.String);
          }
      }
      else if (main_parser.Command == decrypt)
      {
          if (de_help.Switched)
          {
              Console.Write(decrypt.HelpText());
          }
          else
          {
              DoDecrypt(de_input.String, de_output.String);
          }
      }
  }
  ```
  
  > **COMMAND-LINE EXAMPLE:** `program.exe encrypt -i input.txt -o output.txt`
  
  > + The first argument of the command-line will be tested against each nested command. Once one is found, the rest of the arguments will be parsed by the corresponding parser. If the first argument does not match any command, the line will be parsed by the instance's own set of switches.
  > + The nested parameters will be listed by the `HelpText()` of the encompassing parser.
  > + All nested parsers/commands will be executed recursively as needed. The `Parse` method of the nested parsers should not be called.

-----------------------------------------------------------------------------------------------------------

## Classes, Methods and Properties

* **`ParsecsParser`** class

| **Member** | **Syntax** | **Returns** | **Description** |
| :---: | :--- | :---: | :--- |
| `AddOption` | *char shortname, string LongName, string HelpText (optional)* | `ParsecsOption` | Create a simple switch |
| `AddOnOff` | *char ShortName, string LongName, ParsecsState DefaultState, string HelpText (optional)* | `ParsecsOption`  | Create an *on/off* switch | 
| `AddString` | *char ShortName, string LongName, string HelpText (optional)* | `ParsecsOption`  | Create a string parameter |
| `AddChoice` | *char DefaultValue (optional), string HelpText (optional)* | `ParsecsChoice`  | Create a group of mutually exclusive switches |
| `AddCommand` | *string Command, string HelpText (optional)* | `ParsecsParser`  | Create a nested command |
| `HelpText` | *none* | `string` | Build the help text |
| `Parse` | *string[] args* | `bool` | Execute the command-line parsing |
| `GetStrings` | *none* | `Enumerator<string>` | Loose parameters found in the command-line |
| `Command` | *read-only property* | `ParsecsParser` | Points to the nested command parser specified by the user |

-----------------------------------------------------------------------------------------------------------

* **`ParsecsOption`** class

| **Member** | **Syntax** | **Returns** | **Description** |
| :---: | :--- | :---: | :--- |
| `State` | *read-only property* | `ParsecsState` | Final switch state found by the parser |
| `Switched` | *read-only property* | `bool` | Equivalent to `State == ParsecsState.On` |
| `String` | *read-only property* | `string` | String passed as argument to the option |

-----------------------------------------------------------------------------------------------------------

* **`ParsecsChoice`** class

| **Member** | **Syntax** | **Returns** | **Description** |
| :---: | :--- | :---: | :--- |
| `AddItem` | *char ShortName, string LongName, string HelpText (optional)* | `ParsecsOption` | Create a new switch into the mutually-exclusive group |
| `Value` | *read-only property* | `char` | The short-name of the chosen switch, or the default value of the group |

-----------------------------------------------------------------------------------------------------------
