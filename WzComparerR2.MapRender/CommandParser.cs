using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WzComparerR2.MapRender
{
    public class CommandParser
    {
        public CommandParser(IReadOnlyList<ArgSpec> specs, params string[] args)
        {
            this.arguments = args;
            this.argSpecs = new Dictionary<string, ArgSpec>();
            foreach (var spec in specs)
            {
                foreach (var alias in spec.Aliases)
                {
                    this.argSpecs[alias] = spec;
                }
            }
            Parse();
        }

        private readonly string[] arguments;
        private readonly Dictionary<string, ArgSpec> argSpecs;

        private HashSet<string> flags { get; } = new();
        private HashSet<string> unknownFlags { get; } = new();
        private Dictionary<string, List<string>> options { get; } = new();
        private List<string> positionals { get; } = new();

        public string Command { get; private set; }
        public bool HasFlag(string name) => this.flags.Contains(name);
        public List<string> GetOption(string name) => this.options.TryGetValue(name, out var value) ? value : new();
        public string GetPositional(int index) => index >= 0 && index < this.positionals.Count ? this.positionals[index] : null;
        public IReadOnlyCollection<string> UnknownFlags => this.unknownFlags;

        private void Parse()
        {
            if (this.arguments == null || this.arguments.Length == 0) return;

            Command = this.arguments[0];

            for (int i = 1; i < this.arguments.Length; i++)
            {
                var arg = this.arguments[i];
                if (this.argSpecs.TryGetValue(arg, out var spec))
                {
                    switch (spec.Type)
                    {
                        case ArgType.Flag:
                            this.flags.Add(spec.Name);
                            break;

                        case ArgType.Option:
                            List<string> innerArgs = new List<string>();
                            int consumed = 0;
                            for (int count = 0; i + 1 + count < this.arguments.Length && count < spec.OptionArgCount; count++)
                            {
                                var innerArg = this.arguments[i + 1 + count];
                                if (this.argSpecs.ContainsKey(innerArg))
                                {
                                    break;
                                }
                                else
                                {
                                    innerArgs.Add(innerArg);
                                    consumed++;
                                }
                            }

                            for (int left = consumed; left < spec.OptionArgCount; left++)
                            {
                                innerArgs.Add(string.Empty);
                            }

                            this.options[spec.Name] = innerArgs;
                            i += consumed;
                            break;
                    }
                }
                else
                {
                    if (arg.StartsWith("-") && !int.TryParse(arg, out _))
                    {
                        unknownFlags.Add(arg);
                    }
                    else
                    {
                        positionals.Add(arg);
                    }
                }
            }
        }

        public enum ArgType
        {
            Flag,
            Option
        }

        public class ArgSpec
        {
            public string Name;
            public ArgType Type;
            public int OptionArgCount;
            public string[] Aliases;
        }

        #region PreDefined ArgSpecs
        public static readonly IReadOnlyList<ArgSpec> SummonSpecs = new List<ArgSpec>
            {
                new ArgSpec() { Name = "Flip", Type = ArgType.Flag, OptionArgCount = 0, Aliases = new[] { "-f", "--flip" } },
                new ArgSpec() { Name = "Regen", Type = ArgType.Flag, OptionArgCount = 0, Aliases = new[] { "-r", "--regen" } },
            };
        #endregion
    }
}
