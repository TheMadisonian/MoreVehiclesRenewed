// <copyright file="CodeProcessor.cs" company="dymanoid">
// Copyright (c) dymanoid. All rights reserved.
// </copyright>

namespace MoreVehicles.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Reflection.Emit;
    using HarmonyLib;

    /// <summary>
    /// A helper class providing methods for IL code manipulation.
    /// </summary>
    internal static class CodeProcessor
    {
        /// <summary>
        /// Replaces the values of <see cref="int"/> operands for all instructions that support operand of that type.
        /// </summary>
        /// <param name="instructions">A collection of IL instructions to process. Cannot be null.</param>
        /// <param name="oldValue">The old operand value to search for.</param>
        /// <param name="newValue">The new operand value to set.</param>
        ///
        /// <returns>A collection of processed IL code instructions.</returns>
        ///
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="instructions"/> is null.</exception>
        public static IEnumerable<CodeInstruction> ReplaceOperands(IEnumerable<CodeInstruction> instructions, int oldValue, int newValue)
        {
            if (instructions == null)
            {
                throw new ArgumentNullException(nameof(instructions));
            }

            return ReplaceOperandsCore(instructions, oldValue, newValue);
        }

        private static IEnumerable<CodeInstruction> ReplaceOperandsCore(IEnumerable<CodeInstruction> instructions, int oldValue, int newValue)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.opcode.OperandType == OperandType.InlineI
                    && instruction.operand is int intVal
                    && intVal == oldValue)
                {
                    // Full-form ldc.i4 — just swap the operand.
                    instruction.operand = newValue;
                }
                else if (instruction.opcode.OperandType == OperandType.ShortInlineI
                         && instruction.operand is sbyte sbyteVal
                         && (int)sbyteVal == oldValue)
                {
                    // Short-form ldc.i4.s — operand is a single sbyte.
                    if (newValue >= sbyte.MinValue && newValue <= sbyte.MaxValue)
                    {
                        instruction.operand = (sbyte)newValue;
                    }
                    else
                    {
                        // New value doesn't fit in a sbyte; promote to full ldc.i4.
                        instruction.opcode = OpCodes.Ldc_I4;
                        instruction.operand = newValue;
                    }
                }
                else if (instruction.operand == null && GetInlineConstant(instruction.opcode) == oldValue)
                {
                    // Named opcodes ldc.i4.0 … ldc.i4.8 and ldc.i4.m1 carry no
                    // operand — the value is implicit in the opcode itself.
                    if (newValue >= sbyte.MinValue && newValue <= sbyte.MaxValue)
                    {
                        instruction.opcode = OpCodes.Ldc_I4_S;
                        instruction.operand = (sbyte)newValue;
                    }
                    else
                    {
                        instruction.opcode = OpCodes.Ldc_I4;
                        instruction.operand = newValue;
                    }
                }

                yield return instruction;
            }
        }

        /// <summary>
        /// Returns the constant value implied by a named <c>ldc.i4.*</c> opcode,
        /// or <see cref="int.MinValue"/> if the opcode is not one of those.
        /// </summary>
        private static int GetInlineConstant(OpCode opcode)
        {
            if (opcode == OpCodes.Ldc_I4_0)
            {
                return 0;
            }

            if (opcode == OpCodes.Ldc_I4_1)
            {
                return 1;
            }

            if (opcode == OpCodes.Ldc_I4_2)
            {
                return 2;
            }

            if (opcode == OpCodes.Ldc_I4_3)
            {
                return 3;
            }

            if (opcode == OpCodes.Ldc_I4_4)
            {
                return 4;
            }

            if (opcode == OpCodes.Ldc_I4_5)
            {
                return 5;
            }

            if (opcode == OpCodes.Ldc_I4_6)
            {
                return 6;
            }

            if (opcode == OpCodes.Ldc_I4_7)
            {
                return 7;
            }

            if (opcode == OpCodes.Ldc_I4_8)
            {
                return 8;
            }

            if (opcode == OpCodes.Ldc_I4_M1)
            {
                return -1;
            }

            return int.MinValue;
        }
    }
}
