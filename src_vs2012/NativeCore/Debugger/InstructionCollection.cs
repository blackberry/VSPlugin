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
        private List<Instruction> _items;

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
    }
}
