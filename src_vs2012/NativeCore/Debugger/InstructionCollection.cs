using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using BlackBerry.NativeCore.Diagnostics;

namespace BlackBerry.NativeCore.Debugger
{
    public sealed class InstructionCollection
    {
        private readonly List<Instruction> _items;

        public InstructionCollection()
        {
            _items = new List<Instruction>();
        }

        private InstructionCollection(List<Instruction> collection)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");

            _items = collection;
        }

        #region Properties

        /// <summary>
        /// Gets the number of instructions.
        /// </summary>
        public int Count
        {
            get { return _items.Count; }
        }

        /// <summary>
        /// Gets the instruction at specified index.
        /// </summary>
        public Instruction this[int index]
        {
            get
            {
                if (index < 0 || index >= _items.Count)
                    return null;

                return _items[index];
            }
        }

        #endregion

        /// <summary>
        /// Creates the collection of instructions from specified content.
        /// Each instruction is defined inside a single line.
        /// </summary>
        public static InstructionCollection Load(string content)
        {
            if (string.IsNullOrEmpty(content))
                throw new ArgumentNullException("content");

            var result = new List<Instruction>();
            foreach (var line in content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var instruction = Instruction.Load(result.Count, line);
                if (instruction != null)
                {
                    result.Add(instruction);
                }
                else
                {
                    TraceLog.WriteLine("Unable to load instruction from line: \"{0}\"", line);
                }
            }

            return new InstructionCollection(result);
        }

        /// <summary>
        /// Creates a collection with predefined instructions.
        /// </summary>
        public static InstructionCollection Load()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream("BlackBerry.NativeCore.Resources.Instructions.txt"))
            {
                if (stream == null)
                    throw new InvalidProgramException("Resource with GDB instructions not found");

                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return Load(reader.ReadToEnd());
                }
            }
        }

        /// <summary> 
        /// Get the associated instruction ID from the command to be sent to GDB.
        /// </summary>
        /// <param name="command"> Command to be sent to GDB. </param>
        /// <param name="param"> When the instruction code stored in "map" is negative, it means that the commands parameters must be saved to 
        /// be used by the listeningGDB thread. When that happens, update and return variable "param". </param>
        /// <returns> Returns the instruction code for the command to be sent to GDB or -1 in case of an error. It can also return the command 
        /// parameters, as it was described above. </returns>
        public Instruction Find(string command, out string param)
        {
            param = string.Empty;

            if (string.IsNullOrEmpty(command))
                return null;

            // everything separated by space are treated as params:
            int pos = command.IndexOf(' ');
            if (pos != -1)
            {
                param = command.Substring(pos, (command.Length - pos)).Replace(' ', ';');
                command = command.Substring(0, pos);
            }

            foreach (var instruction in _items)
            {
                if (instruction.HasCommand(command))
                {
                    if (!instruction.ExpectsParameter)
                    {
                        param = string.Empty;
                    }

                    return instruction;
                }
            }

            return null;
        }
    }
}
