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

* 1.3.0

  + Making the jump to **netstandard2.0**. No more messy dependencies, and problably no more *.NET Framework* for now.

* 1.2.4

  + Now nested commands can be passed anywhere in the command line (previously they shoud be the first argument). Arguments found before it will be parsed by the encompassing parser/command. Arguments found after it will by parsed by its own parser, while the next command is found. Each command found will be encompassed by the previous one.
  + String capturing verbs can now have its short-named version preceeded by an slash, however, an `:`, `=` or a space is needed beteen the verb and the actual string.

* 1.2.3

  + The `DoubleDash` parameter of the `ParsecsParser` class' creator enables the support of some advanced features such as the ability to group several short-named verbs after a single dash, thus requiring a double-dash (or one slash) for long-named verbs. This is the default behavior. If disabled, *double-dashes won't be supported anywere* and each single-dash or slash corresponds to exactly one verb (either short or long-named), and, consequently, verbs should always be separated by spaces.
  + Added support for *MS-DOS* style of verb grouping with slashes without spaces in-between, much like `rmdir /s/q`. This requires the aforementioned `DoubleDash` parameter to be enabled.

* 1.2.2

  + Added the property `Unparsed` to capture the remainder of the command-line after the parsing interruption token "--".

* 1.2.1

  + Fixed passing string parameters to groups of short-named verbs much like `tar -xf filename`. The string-capturing verb must be passed as the last in the group, otherwise the rest of the group would be captured as its string's value.
  + Removed the package's implicit references, such that *NuGet* won't download droves of unnecessary packages (only *System.Linq* for now).

* 1.2.0

  + Now targeting **netstandard1.0**, ready to be consumed by *.NET Core* projects while remaining compatible with *.NET Framework 4.0* and above (although, in such case, it is recommended to stay on the previous version for now).

* 1.1.5

  + Code completion hints for each class, method, properties and parameters.
  + Added the `Count` property to the `ParsecsOption` and `ParsecsChoice` classes to store the number of times that the switch has been found by the parser over the command line.
  + Added the `HelpTextBuilder` method to expose the `StringBuilder` instance composed by the help text generator, in case you want to complement it with your own information before printing.

* 1.1.4

  + Added an `int`-indexed property to `ParsecsParser` and `ParsecsCommand` class to retrieve their n<sup>th</sup> captured string not related to any defined verb (a.k.a. loose parameter).

* 1.1.3

  + Added an `int`-indexed property to `ParsecsOption` class to retrieve its n<sup>th</sup> captured string.
  + Added the `Option` property to `ParsecsChoice` to retrieve the switch's `ParsecsOption` instance chosen by the user.

* 1.1.2

  + Added `ParsecsCommand` class to avoid erroneous calling of the `Parse` method on nested commands. It is now the return value of the `AddCommand` method. It has all members of `ParsecsParser` except `Parse`. In addition, it has a `Name` property that returns this command's verb.
  + Added indexed properties to get the `State` of each switch straight by either its `ShortName` or `LongName`.
  + Added `LooseParameters` property.
  + Parse will not fail if a verb captured more than the maximum number of strings. Instead, exceeding strings will show up on `LooseParameters`.
  + Arguments that must begin with *dash*, *slash* or *plus sign* but should not be interpreted as a verb can be escaped with `\`.
  + Fixed the selected item's `ParsecsOption` instance from a mutually-exclusive group being set as `Off` instead of `On`.

* 1.1.0

  + Added switches that capture multiple strings.
  + Changed `GetStrings()` to `GetLooseParameters()`.
  + Parse will not fail if a switch don't capture a corresponding string. Instead, its `State` property will return `Undefined`.

* 1.0.1

  + Targeting **net40** instead of **net461** to broaden the library compatibility.

-----------------------------------------------------------------------------------------------------------

## Basic command-line parsing

* Instantiate the `Parsecs` class, define each switch by creating `ParsecsOption` instances with `AddOption` and `AddString` methods, then run the `Parse` method over the arguments' array. Once parsed, each `ParsecsOption` instance should be queried for the result of the corresponding capture.

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
            if ((main_help.Switched) || (!input.Switched))
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

* An alternative, slightly contracted version of the same code.

    ```csharp
    static void Main(string[] args)
    {
        var clp = new ParsecsParser();
        var input = clp.AddString('i', "input", "Input file path");
        var output = clp.AddString('o', "output", "Output file path (optional)");
        clp.AddOption('w', "overwrite", "Overwrite existing file");
        clp.AddOption('h', "help", "Display help text");

        if (clp.Parse(args))
        {
            if (clp['h'] || !clp['i'])
            {
                // user requested the help text or didn't provide a required parameter
                Console.Write(clp.HelpText());
            }
            else
            {
                DoSomething(input[0], output[0], clp['w']);
            }
        }
        else
        {
            // something went wrong in the parsing
        }
    }
    ```

  > + For each switch the `ShortName` parameter can be ommited by passing `default(char)` and the `LongName` parameter can be ommited by passing `null`.
  > + Ommiting the `HelpText` parameter will hide the switch from the generated help text.
  > + The `bool` return value of the `Parse` method usually indicates that an unrecognized verb has been found. More advanced checks should be done by the application's own code.

## Defining *ON/OFF* switches

* Define the *ON/OFF* switches with the `AddOnOff` method, specifying their initial/default state.

  ```csharp
  ParsecsOption archive = main_parser.AddOnOff('a', "archive", ParsecsState.On, "Set the archive attribute");
  ParsecsOption readonly = main_parser.AddOnOff('r', "readonly", ParsecsState.Off, "Set the read-only attribute");
  ParsecsOption hidden = main_parser.AddOnOff('h', "hidden", ParsecsState.Off, "Set the hidden attribute");
  ```

  > + The same switch can be passed several times in the command-line. The final state will be defined by the last capture. The `Count` property tells how many times the user provided the same switch.

## Defining mutually-exclusive switches

* Create an instance of `ParsecsChoice` with the `AddChoice` method. Then add each choice item by creating instances of `ParsecsOption` with the `AddItem` method. The captured result is queried with the `Value` or the `Option` properties of the `ParsecsChoice` instance.

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

  > + If the user does not provide any choice, its default value will be returned by the `Value` property.
  > + If the user provides more than one choice of the same mutually-exclusive group, the `Value` property will reflect the last capture, and the `Count` property will tell how many times each item has been provided.
  > + The corresponding `ParsecsOption` instance of each item can be queried for its individual properties.

## Working with nested commands

* Commands are needed when the application performs more than one distinct function and might need different sets of switches for each one. Create nested commands on the `ParsecsParser` class with the `AddCommand` method. Retrieve the user's selected command by querying the `Command` property of the encompassing `ParsecsParser` instance. Each `ParsecsCommand` instance can have its own set of switches.

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
          if (en_help.Switched) // or just (encrypt['h']), meaning the user typed -h, --help, /h or /help
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
          if (!kg_length.Switched) // the user did not provide the key length
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

  > + ~~The user is expected to provide the command's verb as the first argument. Once a valid command is found, the rest of the arguments will be matched with the command's own set of switches. If the first argument does not match any command, the whole line will be parsed by the encompassing instance's own set of switches.~~ Arguments are parsed by the main parser as usual, until a command is found. This command will be registered in the `Command` property of the main parser and the remaining arguments will be present in the command class' own properties.
  > + If the user does not provide a valid command, the `Command` property will point to its own instance.
  > + The nested commands will be listed by the `HelpText` method of the encompassing instance.
  > + Each command can also have its own set of commands in a recursive fashion.
  > + All nested commands' own commands and switches will be parsed recursively as needed.

-----------------------------------------------------------------------------------------------------------
