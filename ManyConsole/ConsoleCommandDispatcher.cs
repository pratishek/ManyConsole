﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ManyConsole.Internal;

namespace ManyConsole
{
    public class ConsoleCommandDispatcher
    {
        public static int DispatchCommand(IEnumerable<ConsoleCommand> commands, string[] arguments, TextWriter consoleOut)
        {
            ConsoleCommand selectedCommand = null;

            TextWriter console = consoleOut;

            try
            {
                if (arguments.Count() < 1)
                    throw new ConsoleHelpAsException("No arguments specified.");

                foreach(var possibleCommand in commands)
                {
                    if (string.IsNullOrEmpty(possibleCommand.Command))
                    {
                        throw new InvalidOperationException(String.Format(
                            "Command {0} did not define property Command, which must specify its command text.",
                            possibleCommand.GetType().Name));
                    }

                    if (arguments.First().ToLower() == possibleCommand.Command.ToLower())
                    {
                        selectedCommand = possibleCommand;

                        var remainingArguments = selectedCommand.Options.Parse(arguments.Skip(1));

                        selectedCommand.FinishLoadingArguments(remainingArguments.ToArray());
                        break;
                    }
                }

                if (selectedCommand == null)
                    throw new ConsoleHelpAsException("Command name not recognized.");
            }
            catch (Exception e)
            {
                console.WriteLine();
                ConsoleHelpAsException.WriterErrorMessage(e, Console.Out);
                console.WriteLine();

                if (selectedCommand != null)
                {
                    ConsoleHelp.ShowCommandHelp(selectedCommand, console);
                }
                else
                {
                    ConsoleHelp.ShowSummaryOfCommands(commands, console);
                }

                return -1;
            }

            try
            {
                ConsoleHelp.ShowParsedCommand(selectedCommand, console);

                return selectedCommand.Run();
            }
            catch (Exception e)
            {
                console.WriteLine();
                console.WriteLine("Caught unhandled exception: " + e.ToString());
                return -1;
            }
        }

        public static IEnumerable<ConsoleCommand> FindCommandsInSameAssemblyAs(Type typeInSameAssembly)
        {
            var assembly = typeInSameAssembly.Assembly;

            var commandTypes = assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(ConsoleCommand)))
                .OrderBy(t => t.FullName);

            List<ConsoleCommand> result = new List<ConsoleCommand>();

            foreach(var commandType in commandTypes)
            {
                var constructor = commandType.GetConstructor(new Type[] { });

                if (constructor == null)
                    continue;

                result.Add((ConsoleCommand)constructor.Invoke(new object[] { }));
            }

            return result;
        }
    }
}
