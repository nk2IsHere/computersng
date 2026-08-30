# Console and commands

The console is what you see on a computer's screen. Type a line and press Enter.

A line starting with a dot runs a command. Anything else evaluates as JavaScript in the
computer's engine, so `1 + 1`, `System.Id()` or `await Network.RequestHttpString("...")`
work directly and print their result.

## Keys

- Enter runs the line
- Up and Down walk the input history
- Left and Right move the cursor
- Mouse wheel scrolls the log
- Ctrl or Cmd + V pastes the OS clipboard into the input
- Ctrl or Cmd + C copies the current input line to the OS clipboard
- F1 reboots the computer

## Command syntax

Commands can be piped. The result of one command becomes context for the next:

```
.cat a.txt | .write b.txt
```

Arguments split on spaces. Quotes keep spaces inside one argument. Arguments starting
with `{` or `[` are kept verbatim until the matching close bracket, so JSON survives:

```
.peripheral ab12 insert {"machine":{"x":2,"y":7},"itemId":"378"}
```

## Commands

| Command | Usage | What it does |
|---|---|---|
| `.help` | `.help` | Prints all commands |
| `.clear` | `.clear` | Clears the console |
| `.echo` | `.echo <message>` | Prints a message |
| `.id` | `.id` | Prints the computer id |
| `.pwd` | `.pwd` | Prints the working directory |
| `.cd` | `.cd <directory>` | Changes directory |
| `.ls` | `.ls <directory>` | Lists files |
| `.cat` | `.cat <file>` | Prints file content |
| `.touch` | `.touch <file>` | Creates a file |
| `.write` | `.write <file> <content>` | Writes content to a file |
| `.mkdir` | `.mkdir <directory>` | Creates a directory |
| `.cp` | `.cp [--recursive] [--overwrite] <source> <destination>` | Copies |
| `.mv` | `.mv [--recursive] [--overwrite] <source> <destination>` | Moves |
| `.rm` | `.rm [--recursive] <file>` | Removes |
| `.exec` | `.exec <script>` | Runs a script file exporting `async Main(console, context)` |
| `.http` | `.http <method> <url> [--headers k=v ...] [--data <data>]` | HTTP request |
| `.net-addr` | `.net-addr` | Prints this computer's network address |
| `.net-ls` | `.net-ls` | Lists covering routers, discovers endpoints and pings each for its type |
| `.net-send` | `.net-send <address> <text>` | Sends a text message to an address |
| `.router-channel` | `.router-channel <routerAddress> <channel or clear>` | Configures a router and awaits its ack |
| `.peripheral` | `.peripheral <address> <cmd> [json-args]` | Sends any RPC command to a peripheral |
| `.machines` | `.machines <address>` | Lists a machine controller's group |

Commands live in `/Command` on the file system. Drop your own command module there, in
external or persistent storage, and it appears in `.help` within a second.
