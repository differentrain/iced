/*
Copyright (C) 2018-2019 de4dot@gmail.com

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

#if !NO_ENCODER
using System;
using System.Diagnostics;

namespace Iced.Intel {
	partial struct Instruction {
		static OpKind GetImmediateOpKind(Code code, int operand) {
			var handlers = EncoderInternal.OpCodeHandlers.Handlers;
			if ((uint)code >= (uint)handlers.Length)
				throw new ArgumentOutOfRangeException(nameof(code));
			var operands = handlers[(int)code].Operands;
			if ((uint)operand >= (uint)operands.Length)
				throw new ArgumentOutOfRangeException(nameof(operand), $"{code} doesn't have at least {operand + 1} operands");
			var opKind = operands[operand].GetImmediateOpKind();
			if (opKind == (OpKind)(-1))
				throw new ArgumentException($"{code}'s op{operand} isn't an immediate operand");
			return opKind;
		}

		static OpKind GetNearBranchOpKind(Code code, int operand) {
			var handlers = EncoderInternal.OpCodeHandlers.Handlers;
			if ((uint)code >= (uint)handlers.Length)
				throw new ArgumentOutOfRangeException(nameof(code));
			var operands = handlers[(int)code].Operands;
			if ((uint)operand >= (uint)operands.Length)
				throw new ArgumentOutOfRangeException(nameof(operand), $"{code} doesn't have at least {operand + 1} operands");
			var opKind = operands[operand].GetNearBranchOpKind();
			if (opKind == (OpKind)(-1))
				throw new ArgumentException($"{code}'s op{operand} isn't a near branch operand");
			return opKind;
		}

		static OpKind GetFarBranchOpKind(Code code, int operand) {
			var handlers = EncoderInternal.OpCodeHandlers.Handlers;
			if ((uint)code >= (uint)handlers.Length)
				throw new ArgumentOutOfRangeException(nameof(code));
			var operands = handlers[(int)code].Operands;
			if ((uint)operand >= (uint)operands.Length)
				throw new ArgumentOutOfRangeException(nameof(operand), $"{code} doesn't have at least {operand + 1} operands");
			var opKind = operands[operand].GetFarBranchOpKind();
			if (opKind == (OpKind)(-1))
				throw new ArgumentException($"{code}'s op{operand} isn't a far branch operand");
			return opKind;
		}

		/// <summary>
		/// Creates a new <see cref="Instruction"/> with no operands
		/// </summary>
		/// <param name="code">Code value</param>
		/// <returns></returns>
		public static Instruction Create(Code code) {
			Instruction instruction = default;
			instruction.Code = code;

			Debug.Assert(instruction.OpCount == 0);
			return instruction;
		}

		/// <summary>
		/// Creates a new near/short branch <see cref="Instruction"/>
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="target">Target address</param>
		/// <returns></returns>
		public static Instruction CreateBranch(Code code, ulong target) {
			Instruction instruction = default;
			instruction.Code = code;

			instruction.Op0Kind = GetNearBranchOpKind(code, 0);
			instruction.NearBranch64 = target;

			Debug.Assert(instruction.OpCount == 1);
			return instruction;
		}

		/// <summary>
		/// Creates a new far branch <see cref="Instruction"/>
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="selector">Selector/segment value</param>
		/// <param name="offset">Offset</param>
		/// <returns></returns>
		public static Instruction CreateBranch(Code code, ushort selector, uint offset) {
			Instruction instruction = default;
			instruction.Code = code;

			instruction.Op0Kind = GetFarBranchOpKind(code, 0);
			instruction.FarBranchSelector = selector;
			instruction.FarBranch32 = offset;

			Debug.Assert(instruction.OpCount == 1);
			return instruction;
		}

		/// <summary>
		/// Creates an instruction with a 64-bit memory offset as the second operand, eg. 'mov al,[123456789ABCDEF0]'
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="register">Register (al, ax, eax, rax)</param>
		/// <param name="address">64-bit address</param>
		/// <param name="prefixSegment">Segment override or <see cref="Register.None"/></param>
		/// <returns></returns>
		public static Instruction CreateMemory64(Code code, Register register, ulong address, Register prefixSegment = Register.None) {
			Instruction instruction = default;
			instruction.Code = code;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp0Kind = OpKind.Register;
			instruction.InternalOp0Register = register;

			instruction.InternalOp1Kind = OpKind.Memory64;
			instruction.MemoryAddress64 = address;
			instruction.InternalSetMemoryDisplSize(4);
			instruction.SegmentPrefix = prefixSegment;

			Debug.Assert(instruction.OpCount == 2);
			return instruction;
		}

		/// <summary>
		/// Creates an instruction with a 64-bit memory offset as the first operand, eg. 'mov [123456789ABCDEF0],al'
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="address">64-bit address</param>
		/// <param name="register">Register (al, ax, eax, rax)</param>
		/// <param name="prefixSegment">Segment override or <see cref="Register.None"/></param>
		/// <returns></returns>
		public static Instruction CreateMemory64(Code code, ulong address, Register register, Register prefixSegment = Register.None) {
			Instruction instruction = default;
			instruction.Code = code;

			instruction.InternalOp0Kind = OpKind.Memory64;
			instruction.MemoryAddress64 = address;
			instruction.InternalSetMemoryDisplSize(4);
			instruction.SegmentPrefix = prefixSegment;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp1Kind = OpKind.Register;
			instruction.InternalOp1Register = register;

			Debug.Assert(instruction.OpCount == 2);
			return instruction;
		}

		/// <summary>
		/// Creates an instruction
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="register">Register</param>
		/// <returns></returns>
		public static Instruction Create(Code code, Register register) {
			Instruction instruction = default;
			instruction.Code = code;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp0Kind = OpKind.Register;
			instruction.InternalOp0Register = register;

			Debug.Assert(instruction.OpCount == 1);
			return instruction;
		}

		/// <summary>
		/// Creates an instruction
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="immediate">Immediate</param>
		/// <returns></returns>
		public static Instruction Create(Code code, int immediate) {
			Instruction instruction = default;
			instruction.Code = code;

			instruction.InternalOp0Kind = GetImmediateOpKind(code, 0);
			instruction.Immediate32 = (uint)immediate;

			Debug.Assert(instruction.OpCount == 1);
			return instruction;
		}

		/// <summary>
		/// Creates an instruction
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="immediate">Immediate</param>
		/// <returns></returns>
		public static Instruction Create(Code code, uint immediate) =>
			Create(code, (int)immediate);

		/// <summary>
		/// Creates an instruction
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="memory">Memory operand</param>
		/// <returns></returns>
		public static Instruction Create(Code code, in MemoryOperand memory) {
			Instruction instruction = default;
			instruction.Code = code;

			instruction.InternalOp0Kind = OpKind.Memory;
			instruction.InternalMemoryBase = memory.Base;
			instruction.InternalMemoryIndex = memory.Index;
			instruction.MemoryIndexScale = memory.Scale;
			instruction.MemoryDisplSize = memory.DisplSize;
			instruction.MemoryDisplacement = (uint)memory.Displacement;
			instruction.IsBroadcast = memory.IsBroadcast;
			instruction.SegmentPrefix = memory.SegmentPrefix;

			Debug.Assert(instruction.OpCount == 1);
			return instruction;
		}

		/// <summary>
		/// Creates an instruction
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="register1">Register</param>
		/// <param name="register2">Register</param>
		/// <returns></returns>
		public static Instruction Create(Code code, Register register1, Register register2) {
			Instruction instruction = default;
			instruction.Code = code;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp0Kind = OpKind.Register;
			instruction.InternalOp0Register = register1;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp1Kind = OpKind.Register;
			instruction.InternalOp1Register = register2;

			Debug.Assert(instruction.OpCount == 2);
			return instruction;
		}

		/// <summary>
		/// Creates an instruction
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="register">Register</param>
		/// <param name="immediate">Immediate</param>
		/// <returns></returns>
		public static Instruction Create(Code code, Register register, int immediate) {
			Instruction instruction = default;
			instruction.Code = code;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp0Kind = OpKind.Register;
			instruction.InternalOp0Register = register;

			var opKind = GetImmediateOpKind(code, 1);
			instruction.InternalOp1Kind = opKind;
			if (opKind == OpKind.Immediate64)
				instruction.Immediate64 = (ulong)immediate;
			else
				instruction.Immediate32 = (uint)immediate;

			Debug.Assert(instruction.OpCount == 2);
			return instruction;
		}

		/// <summary>
		/// Creates an instruction
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="register">Register</param>
		/// <param name="immediate">Immediate</param>
		/// <returns></returns>
		public static Instruction Create(Code code, Register register, uint immediate) {
			Instruction instruction = default;
			instruction.Code = code;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp0Kind = OpKind.Register;
			instruction.InternalOp0Register = register;

			var opKind = GetImmediateOpKind(code, 1);
			instruction.InternalOp1Kind = opKind;
			if (opKind == OpKind.Immediate64)
				instruction.Immediate64 = immediate;
			else
				instruction.Immediate32 = immediate;

			Debug.Assert(instruction.OpCount == 2);
			return instruction;
		}

		/// <summary>
		/// Creates an instruction with a 64-bit immediate value
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="register">Register</param>
		/// <param name="immediate">64-bit immediate</param>
		/// <returns></returns>
		public static Instruction Create(Code code, Register register, long immediate) {
			Instruction instruction = default;
			instruction.Code = code;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp0Kind = OpKind.Register;
			instruction.InternalOp0Register = register;

			instruction.InternalOp1Kind = OpKind.Immediate64;
			instruction.Immediate64 = (ulong)immediate;

			Debug.Assert(instruction.OpCount == 2);
			return instruction;
		}

		/// <summary>
		/// Creates an instruction with a 64-bit immediate value
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="register">Register</param>
		/// <param name="immediate">64-bit immediate</param>
		/// <returns></returns>
		public static Instruction Create(Code code, Register register, ulong immediate) =>
			Create(code, register, (long)immediate);

		/// <summary>
		/// Creates an instruction
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="register">Register</param>
		/// <param name="memory">Memory operand</param>
		/// <returns></returns>
		public static Instruction Create(Code code, Register register, in MemoryOperand memory) {
			Instruction instruction = default;
			instruction.Code = code;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp0Kind = OpKind.Register;
			instruction.InternalOp0Register = register;

			instruction.InternalOp1Kind = OpKind.Memory;
			instruction.InternalMemoryBase = memory.Base;
			instruction.InternalMemoryIndex = memory.Index;
			instruction.MemoryIndexScale = memory.Scale;
			instruction.MemoryDisplSize = memory.DisplSize;
			instruction.MemoryDisplacement = (uint)memory.Displacement;
			instruction.IsBroadcast = memory.IsBroadcast;
			instruction.SegmentPrefix = memory.SegmentPrefix;

			Debug.Assert(instruction.OpCount == 2);
			return instruction;
		}

		/// <summary>
		/// Creates an instruction
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="immediate">Immediate</param>
		/// <param name="register">Register</param>
		/// <returns></returns>
		public static Instruction Create(Code code, int immediate, Register register) {
			Instruction instruction = default;
			instruction.Code = code;

			instruction.InternalOp0Kind = GetImmediateOpKind(code, 0);
			instruction.Immediate32 = (uint)immediate;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp1Kind = OpKind.Register;
			instruction.InternalOp1Register = register;

			Debug.Assert(instruction.OpCount == 2);
			return instruction;
		}

		/// <summary>
		/// Creates an instruction
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="immediate">Immediate</param>
		/// <param name="register">Register</param>
		/// <returns></returns>
		public static Instruction Create(Code code, uint immediate, Register register) =>
			Create(code, (int)immediate, register);

		/// <summary>
		/// Creates an instruction
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="immediate">Immediate</param>
		/// <param name="immediate2">Second immediate</param>
		/// <returns></returns>
		public static Instruction Create(Code code, int immediate, byte immediate2) {
			Instruction instruction = default;
			instruction.Code = code;

			instruction.InternalOp0Kind = GetImmediateOpKind(code, 0);
			instruction.Immediate32 = (uint)immediate;

			instruction.InternalOp1Kind = OpKind.Immediate8_2nd;
			instruction.Immediate8_2nd = immediate2;

			Debug.Assert(instruction.OpCount == 2);
			return instruction;
		}

		/// <summary>
		/// Creates an instruction
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="immediate">Immediate</param>
		/// <param name="immediate2">Second immediate</param>
		/// <returns></returns>
		public static Instruction Create(Code code, uint immediate, byte immediate2) =>
			Create(code, (int)immediate, immediate2);

		/// <summary>
		/// Creates an instruction
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="memory">Memory operand</param>
		/// <param name="register">Register</param>
		/// <returns></returns>
		public static Instruction Create(Code code, in MemoryOperand memory, Register register) {
			Instruction instruction = default;
			instruction.Code = code;

			instruction.InternalOp0Kind = OpKind.Memory;
			instruction.InternalMemoryBase = memory.Base;
			instruction.InternalMemoryIndex = memory.Index;
			instruction.MemoryIndexScale = memory.Scale;
			instruction.MemoryDisplSize = memory.DisplSize;
			instruction.MemoryDisplacement = (uint)memory.Displacement;
			instruction.IsBroadcast = memory.IsBroadcast;
			instruction.SegmentPrefix = memory.SegmentPrefix;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp1Kind = OpKind.Register;
			instruction.InternalOp1Register = register;

			Debug.Assert(instruction.OpCount == 2);
			return instruction;
		}

		/// <summary>
		/// Creates an instruction
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="memory">Memory operand</param>
		/// <param name="immediate">Immediate</param>
		/// <returns></returns>
		public static Instruction Create(Code code, in MemoryOperand memory, int immediate) {
			Instruction instruction = default;
			instruction.Code = code;

			instruction.InternalOp0Kind = OpKind.Memory;
			instruction.InternalMemoryBase = memory.Base;
			instruction.InternalMemoryIndex = memory.Index;
			instruction.MemoryIndexScale = memory.Scale;
			instruction.MemoryDisplSize = memory.DisplSize;
			instruction.MemoryDisplacement = (uint)memory.Displacement;
			instruction.IsBroadcast = memory.IsBroadcast;
			instruction.SegmentPrefix = memory.SegmentPrefix;

			instruction.InternalOp1Kind = GetImmediateOpKind(code, 1);
			instruction.Immediate32 = (uint)immediate;

			Debug.Assert(instruction.OpCount == 2);
			return instruction;
		}

		/// <summary>
		/// Creates an instruction
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="memory">Memory operand</param>
		/// <param name="immediate">Immediate</param>
		/// <returns></returns>
		public static Instruction Create(Code code, in MemoryOperand memory, uint immediate) =>
			Create(code, memory, (int)immediate);

		/// <summary>
		/// Creates an instruction
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="register1">Register</param>
		/// <param name="register2">Register</param>
		/// <param name="register3">Register</param>
		/// <returns></returns>
		public static Instruction Create(Code code, Register register1, Register register2, Register register3) {
			Instruction instruction = default;
			instruction.Code = code;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp0Kind = OpKind.Register;
			instruction.InternalOp0Register = register1;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp1Kind = OpKind.Register;
			instruction.InternalOp1Register = register2;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp2Kind = OpKind.Register;
			instruction.InternalOp2Register = register3;

			Debug.Assert(instruction.OpCount == 3);
			return instruction;
		}

		/// <summary>
		/// Creates an instruction
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="register1">Register</param>
		/// <param name="register2">Register</param>
		/// <param name="immediate">Immediate</param>
		/// <returns></returns>
		public static Instruction Create(Code code, Register register1, Register register2, int immediate) {
			Instruction instruction = default;
			instruction.Code = code;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp0Kind = OpKind.Register;
			instruction.InternalOp0Register = register1;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp1Kind = OpKind.Register;
			instruction.InternalOp1Register = register2;

			instruction.InternalOp2Kind = GetImmediateOpKind(code, 2);
			instruction.Immediate32 = (uint)immediate;

			Debug.Assert(instruction.OpCount == 3);
			return instruction;
		}

		/// <summary>
		/// Creates an instruction
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="register1">Register</param>
		/// <param name="register2">Register</param>
		/// <param name="immediate">Immediate</param>
		/// <returns></returns>
		public static Instruction Create(Code code, Register register1, Register register2, uint immediate) =>
			Create(code, register1, register2, (int)immediate);

		/// <summary>
		/// Creates an instruction
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="register1">Register</param>
		/// <param name="register2">Register</param>
		/// <param name="memory">Memory operand</param>
		/// <returns></returns>
		public static Instruction Create(Code code, Register register1, Register register2, in MemoryOperand memory) {
			Instruction instruction = default;
			instruction.Code = code;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp0Kind = OpKind.Register;
			instruction.InternalOp0Register = register1;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp1Kind = OpKind.Register;
			instruction.InternalOp1Register = register2;

			instruction.InternalOp2Kind = OpKind.Memory;
			instruction.InternalMemoryBase = memory.Base;
			instruction.InternalMemoryIndex = memory.Index;
			instruction.MemoryIndexScale = memory.Scale;
			instruction.MemoryDisplSize = memory.DisplSize;
			instruction.MemoryDisplacement = (uint)memory.Displacement;
			instruction.IsBroadcast = memory.IsBroadcast;
			instruction.SegmentPrefix = memory.SegmentPrefix;

			Debug.Assert(instruction.OpCount == 3);
			return instruction;
		}

		/// <summary>
		/// Creates an instruction
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="register">Register</param>
		/// <param name="immediate">Immediate</param>
		/// <param name="immediate2">Second immediate</param>
		/// <returns></returns>
		public static Instruction Create(Code code, Register register, int immediate, byte immediate2) {
			Instruction instruction = default;
			instruction.Code = code;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp0Kind = OpKind.Register;
			instruction.InternalOp0Register = register;

			instruction.InternalOp1Kind = GetImmediateOpKind(code, 1);
			instruction.Immediate32 = (uint)immediate;

			instruction.InternalOp2Kind = OpKind.Immediate8_2nd;
			instruction.Immediate8_2nd = immediate2;

			Debug.Assert(instruction.OpCount == 3);
			return instruction;
		}

		/// <summary>
		/// Creates an instruction
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="register">Register</param>
		/// <param name="immediate">Immediate</param>
		/// <param name="immediate2">Second immediate</param>
		/// <returns></returns>
		public static Instruction Create(Code code, Register register, uint immediate, byte immediate2) =>
			Create(code, register, (int)immediate, immediate2);

		/// <summary>
		/// Creates an instruction
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="register1">Register</param>
		/// <param name="memory">Memory operand</param>
		/// <param name="register2">Register</param>
		/// <returns></returns>
		public static Instruction Create(Code code, Register register1, in MemoryOperand memory, Register register2) {
			Instruction instruction = default;
			instruction.Code = code;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp0Kind = OpKind.Register;
			instruction.InternalOp0Register = register1;

			instruction.InternalOp1Kind = OpKind.Memory;
			instruction.InternalMemoryBase = memory.Base;
			instruction.InternalMemoryIndex = memory.Index;
			instruction.MemoryIndexScale = memory.Scale;
			instruction.MemoryDisplSize = memory.DisplSize;
			instruction.MemoryDisplacement = (uint)memory.Displacement;
			instruction.IsBroadcast = memory.IsBroadcast;
			instruction.SegmentPrefix = memory.SegmentPrefix;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp2Kind = OpKind.Register;
			instruction.InternalOp2Register = register2;

			Debug.Assert(instruction.OpCount == 3);
			return instruction;
		}

		/// <summary>
		/// Creates an instruction
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="register">Register</param>
		/// <param name="memory">Memory operand</param>
		/// <param name="immediate">Immediate</param>
		/// <returns></returns>
		public static Instruction Create(Code code, Register register, in MemoryOperand memory, int immediate) {
			Instruction instruction = default;
			instruction.Code = code;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp0Kind = OpKind.Register;
			instruction.InternalOp0Register = register;

			instruction.InternalOp1Kind = OpKind.Memory;
			instruction.InternalMemoryBase = memory.Base;
			instruction.InternalMemoryIndex = memory.Index;
			instruction.MemoryIndexScale = memory.Scale;
			instruction.MemoryDisplSize = memory.DisplSize;
			instruction.MemoryDisplacement = (uint)memory.Displacement;
			instruction.IsBroadcast = memory.IsBroadcast;
			instruction.SegmentPrefix = memory.SegmentPrefix;

			instruction.InternalOp2Kind = GetImmediateOpKind(code, 2);
			instruction.Immediate32 = (uint)immediate;

			Debug.Assert(instruction.OpCount == 3);
			return instruction;
		}

		/// <summary>
		/// Creates an instruction
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="register">Register</param>
		/// <param name="memory">Memory operand</param>
		/// <param name="immediate">Immediate</param>
		/// <returns></returns>
		public static Instruction Create(Code code, Register register, in MemoryOperand memory, uint immediate) =>
			Create(code, register, memory, (int)immediate);

		/// <summary>
		/// Creates an instruction
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="memory">Memory operand</param>
		/// <param name="register1">Register</param>
		/// <param name="register2">Register</param>
		/// <returns></returns>
		public static Instruction Create(Code code, in MemoryOperand memory, Register register1, Register register2) {
			Instruction instruction = default;
			instruction.Code = code;

			instruction.InternalOp0Kind = OpKind.Memory;
			instruction.InternalMemoryBase = memory.Base;
			instruction.InternalMemoryIndex = memory.Index;
			instruction.MemoryIndexScale = memory.Scale;
			instruction.MemoryDisplSize = memory.DisplSize;
			instruction.MemoryDisplacement = (uint)memory.Displacement;
			instruction.IsBroadcast = memory.IsBroadcast;
			instruction.SegmentPrefix = memory.SegmentPrefix;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp1Kind = OpKind.Register;
			instruction.InternalOp1Register = register1;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp2Kind = OpKind.Register;
			instruction.InternalOp2Register = register2;

			Debug.Assert(instruction.OpCount == 3);
			return instruction;
		}

		/// <summary>
		/// Creates an instruction
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="memory">Memory operand</param>
		/// <param name="register">Register</param>
		/// <param name="immediate">Immediate</param>
		/// <returns></returns>
		public static Instruction Create(Code code, in MemoryOperand memory, Register register, int immediate) {
			Instruction instruction = default;
			instruction.Code = code;

			instruction.InternalOp0Kind = OpKind.Memory;
			instruction.InternalMemoryBase = memory.Base;
			instruction.InternalMemoryIndex = memory.Index;
			instruction.MemoryIndexScale = memory.Scale;
			instruction.MemoryDisplSize = memory.DisplSize;
			instruction.MemoryDisplacement = (uint)memory.Displacement;
			instruction.IsBroadcast = memory.IsBroadcast;
			instruction.SegmentPrefix = memory.SegmentPrefix;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp1Kind = OpKind.Register;
			instruction.InternalOp1Register = register;

			instruction.InternalOp2Kind = GetImmediateOpKind(code, 2);
			instruction.Immediate32 = (uint)immediate;

			Debug.Assert(instruction.OpCount == 3);
			return instruction;
		}

		/// <summary>
		/// Creates an instruction
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="memory">Memory operand</param>
		/// <param name="register">Register</param>
		/// <param name="immediate">Immediate</param>
		/// <returns></returns>
		public static Instruction Create(Code code, in MemoryOperand memory, Register register, uint immediate) =>
			Create(code, memory, register, (int)immediate);

		/// <summary>
		/// Creates an instruction
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="register1">Register</param>
		/// <param name="register2">Register</param>
		/// <param name="register3">Register</param>
		/// <param name="register4">Register</param>
		/// <returns></returns>
		public static Instruction Create(Code code, Register register1, Register register2, Register register3, Register register4) {
			Instruction instruction = default;
			instruction.Code = code;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp0Kind = OpKind.Register;
			instruction.InternalOp0Register = register1;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp1Kind = OpKind.Register;
			instruction.InternalOp1Register = register2;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp2Kind = OpKind.Register;
			instruction.InternalOp2Register = register3;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp3Kind = OpKind.Register;
			instruction.InternalOp3Register = register4;

			Debug.Assert(instruction.OpCount == 4);
			return instruction;
		}

		/// <summary>
		/// Creates an instruction
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="register1">Register</param>
		/// <param name="register2">Register</param>
		/// <param name="register3">Register</param>
		/// <param name="immediate">Immediate</param>
		/// <returns></returns>
		public static Instruction Create(Code code, Register register1, Register register2, Register register3, int immediate) {
			Instruction instruction = default;
			instruction.Code = code;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp0Kind = OpKind.Register;
			instruction.InternalOp0Register = register1;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp1Kind = OpKind.Register;
			instruction.InternalOp1Register = register2;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp2Kind = OpKind.Register;
			instruction.InternalOp2Register = register3;

			instruction.InternalOp3Kind = GetImmediateOpKind(code, 3);
			instruction.Immediate32 = (uint)immediate;

			Debug.Assert(instruction.OpCount == 4);
			return instruction;
		}

		/// <summary>
		/// Creates an instruction
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="register1">Register</param>
		/// <param name="register2">Register</param>
		/// <param name="register3">Register</param>
		/// <param name="immediate">Immediate</param>
		/// <returns></returns>
		public static Instruction Create(Code code, Register register1, Register register2, Register register3, uint immediate) =>
			Create(code, register1, register2, register3, (int)immediate);

		/// <summary>
		/// Creates an instruction
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="register1">Register</param>
		/// <param name="register2">Register</param>
		/// <param name="register3">Register</param>
		/// <param name="memory">Memory operand</param>
		/// <returns></returns>
		public static Instruction Create(Code code, Register register1, Register register2, Register register3, in MemoryOperand memory) {
			Instruction instruction = default;
			instruction.Code = code;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp0Kind = OpKind.Register;
			instruction.InternalOp0Register = register1;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp1Kind = OpKind.Register;
			instruction.InternalOp1Register = register2;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp2Kind = OpKind.Register;
			instruction.InternalOp2Register = register3;

			instruction.InternalOp3Kind = OpKind.Memory;
			instruction.InternalMemoryBase = memory.Base;
			instruction.InternalMemoryIndex = memory.Index;
			instruction.MemoryIndexScale = memory.Scale;
			instruction.MemoryDisplSize = memory.DisplSize;
			instruction.MemoryDisplacement = (uint)memory.Displacement;
			instruction.IsBroadcast = memory.IsBroadcast;
			instruction.SegmentPrefix = memory.SegmentPrefix;

			Debug.Assert(instruction.OpCount == 4);
			return instruction;
		}

		/// <summary>
		/// Creates an instruction
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="register1">Register</param>
		/// <param name="register2">Register</param>
		/// <param name="immediate">Immediate</param>
		/// <param name="immediate2">Second immediate</param>
		/// <returns></returns>
		public static Instruction Create(Code code, Register register1, Register register2, int immediate, byte immediate2) {
			Instruction instruction = default;
			instruction.Code = code;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp0Kind = OpKind.Register;
			instruction.InternalOp0Register = register1;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp1Kind = OpKind.Register;
			instruction.InternalOp1Register = register2;

			instruction.InternalOp2Kind = GetImmediateOpKind(code, 2);
			instruction.Immediate32 = (uint)immediate;

			instruction.InternalOp3Kind = OpKind.Immediate8_2nd;
			instruction.Immediate8_2nd = immediate2;

			Debug.Assert(instruction.OpCount == 4);
			return instruction;
		}

		/// <summary>
		/// Creates an instruction
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="register1">Register</param>
		/// <param name="register2">Register</param>
		/// <param name="immediate">Immediate</param>
		/// <param name="immediate2">Second immediate</param>
		/// <returns></returns>
		public static Instruction Create(Code code, Register register1, Register register2, uint immediate, byte immediate2) =>
			Create(code, register1, register2, (int)immediate, immediate2);

		/// <summary>
		/// Creates an instruction
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="register1">Register</param>
		/// <param name="register2">Register</param>
		/// <param name="memory">Memory operand</param>
		/// <param name="register3">Register</param>
		/// <returns></returns>
		public static Instruction Create(Code code, Register register1, Register register2, in MemoryOperand memory, Register register3) {
			Instruction instruction = default;
			instruction.Code = code;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp0Kind = OpKind.Register;
			instruction.InternalOp0Register = register1;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp1Kind = OpKind.Register;
			instruction.InternalOp1Register = register2;

			instruction.InternalOp2Kind = OpKind.Memory;
			instruction.InternalMemoryBase = memory.Base;
			instruction.InternalMemoryIndex = memory.Index;
			instruction.MemoryIndexScale = memory.Scale;
			instruction.MemoryDisplSize = memory.DisplSize;
			instruction.MemoryDisplacement = (uint)memory.Displacement;
			instruction.IsBroadcast = memory.IsBroadcast;
			instruction.SegmentPrefix = memory.SegmentPrefix;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp3Kind = OpKind.Register;
			instruction.InternalOp3Register = register3;

			Debug.Assert(instruction.OpCount == 4);
			return instruction;
		}

		/// <summary>
		/// Creates an instruction
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="register1">Register</param>
		/// <param name="register2">Register</param>
		/// <param name="memory">Memory operand</param>
		/// <param name="immediate">Immediate</param>
		/// <returns></returns>
		public static Instruction Create(Code code, Register register1, Register register2, in MemoryOperand memory, int immediate) {
			Instruction instruction = default;
			instruction.Code = code;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp0Kind = OpKind.Register;
			instruction.InternalOp0Register = register1;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp1Kind = OpKind.Register;
			instruction.InternalOp1Register = register2;

			instruction.InternalOp2Kind = OpKind.Memory;
			instruction.InternalMemoryBase = memory.Base;
			instruction.InternalMemoryIndex = memory.Index;
			instruction.MemoryIndexScale = memory.Scale;
			instruction.MemoryDisplSize = memory.DisplSize;
			instruction.MemoryDisplacement = (uint)memory.Displacement;
			instruction.IsBroadcast = memory.IsBroadcast;
			instruction.SegmentPrefix = memory.SegmentPrefix;

			instruction.InternalOp3Kind = GetImmediateOpKind(code, 3);
			instruction.Immediate32 = (uint)immediate;

			Debug.Assert(instruction.OpCount == 4);
			return instruction;
		}

		/// <summary>
		/// Creates an instruction
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="register1">Register</param>
		/// <param name="register2">Register</param>
		/// <param name="memory">Memory operand</param>
		/// <param name="immediate">Immediate</param>
		/// <returns></returns>
		public static Instruction Create(Code code, Register register1, Register register2, in MemoryOperand memory, uint immediate) =>
			Create(code, register1, register2, memory, (int)immediate);

		/// <summary>
		/// Creates an instruction
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="register1">Register</param>
		/// <param name="register2">Register</param>
		/// <param name="register3">Register</param>
		/// <param name="register4">Register</param>
		/// <param name="immediate">Immediate</param>
		/// <returns></returns>
		public static Instruction Create(Code code, Register register1, Register register2, Register register3, Register register4, int immediate) {
			Instruction instruction = default;
			instruction.Code = code;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp0Kind = OpKind.Register;
			instruction.InternalOp0Register = register1;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp1Kind = OpKind.Register;
			instruction.InternalOp1Register = register2;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp2Kind = OpKind.Register;
			instruction.InternalOp2Register = register3;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp3Kind = OpKind.Register;
			instruction.InternalOp3Register = register4;

			instruction.InternalOp4Kind = GetImmediateOpKind(code, 4);
			instruction.Immediate32 = (uint)immediate;

			Debug.Assert(instruction.OpCount == 5);
			return instruction;
		}

		/// <summary>
		/// Creates an instruction
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="register1">Register</param>
		/// <param name="register2">Register</param>
		/// <param name="register3">Register</param>
		/// <param name="register4">Register</param>
		/// <param name="immediate">Immediate</param>
		/// <returns></returns>
		public static Instruction Create(Code code, Register register1, Register register2, Register register3, Register register4, uint immediate) =>
			Create(code, register1, register2, register3, register4, (int)immediate);

		/// <summary>
		/// Creates an instruction
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="register1">Register</param>
		/// <param name="register2">Register</param>
		/// <param name="register3">Register</param>
		/// <param name="memory">Memory operand</param>
		/// <param name="immediate">Immediate</param>
		/// <returns></returns>
		public static Instruction Create(Code code, Register register1, Register register2, Register register3, in MemoryOperand memory, int immediate) {
			Instruction instruction = default;
			instruction.Code = code;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp0Kind = OpKind.Register;
			instruction.InternalOp0Register = register1;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp1Kind = OpKind.Register;
			instruction.InternalOp1Register = register2;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp2Kind = OpKind.Register;
			instruction.InternalOp2Register = register3;

			instruction.InternalOp3Kind = OpKind.Memory;
			instruction.InternalMemoryBase = memory.Base;
			instruction.InternalMemoryIndex = memory.Index;
			instruction.MemoryIndexScale = memory.Scale;
			instruction.MemoryDisplSize = memory.DisplSize;
			instruction.MemoryDisplacement = (uint)memory.Displacement;
			instruction.IsBroadcast = memory.IsBroadcast;
			instruction.SegmentPrefix = memory.SegmentPrefix;

			instruction.InternalOp4Kind = GetImmediateOpKind(code, 4);
			instruction.Immediate32 = (uint)immediate;

			Debug.Assert(instruction.OpCount == 5);
			return instruction;
		}

		/// <summary>
		/// Creates an instruction
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="register1">Register</param>
		/// <param name="register2">Register</param>
		/// <param name="register3">Register</param>
		/// <param name="memory">Memory operand</param>
		/// <param name="immediate">Immediate</param>
		/// <returns></returns>
		public static Instruction Create(Code code, Register register1, Register register2, Register register3, in MemoryOperand memory, uint immediate) =>
			Create(code, register1, register2, register3, memory, (int)immediate);

		/// <summary>
		/// Creates an instruction
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="register1">Register</param>
		/// <param name="register2">Register</param>
		/// <param name="memory">Memory operand</param>
		/// <param name="register3">Register</param>
		/// <param name="immediate">Immediate</param>
		/// <returns></returns>
		public static Instruction Create(Code code, Register register1, Register register2, in MemoryOperand memory, Register register3, int immediate) {
			Instruction instruction = default;
			instruction.Code = code;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp0Kind = OpKind.Register;
			instruction.InternalOp0Register = register1;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp1Kind = OpKind.Register;
			instruction.InternalOp1Register = register2;

			instruction.InternalOp2Kind = OpKind.Memory;
			instruction.InternalMemoryBase = memory.Base;
			instruction.InternalMemoryIndex = memory.Index;
			instruction.MemoryIndexScale = memory.Scale;
			instruction.MemoryDisplSize = memory.DisplSize;
			instruction.MemoryDisplacement = (uint)memory.Displacement;
			instruction.IsBroadcast = memory.IsBroadcast;
			instruction.SegmentPrefix = memory.SegmentPrefix;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp3Kind = OpKind.Register;
			instruction.InternalOp3Register = register3;

			instruction.InternalOp4Kind = GetImmediateOpKind(code, 4);
			instruction.Immediate32 = (uint)immediate;

			Debug.Assert(instruction.OpCount == 5);
			return instruction;
		}

		/// <summary>
		/// Creates an instruction
		/// </summary>
		/// <param name="code">Code value</param>
		/// <param name="register1">Register</param>
		/// <param name="register2">Register</param>
		/// <param name="memory">Memory operand</param>
		/// <param name="register3">Register</param>
		/// <param name="immediate">Immediate</param>
		/// <returns></returns>
		public static Instruction Create(Code code, Register register1, Register register2, in MemoryOperand memory, Register register3, uint immediate) =>
			Create(code, register1, register2, memory, register3, (int)immediate);

		/// <summary>
		/// Creates an outsb instruction
		/// </summary>
		/// <param name="addressSize">16, 32, or 64</param>
		/// <param name="prefixSegment">Segment override or <see cref="Register.None"/></param>
		/// <param name="repPrefix">Rep prefix</param>
		/// <returns></returns>
		public static Instruction CreateOutsb(int addressSize, Register prefixSegment = Register.None, RepPrefixKind repPrefix = RepPrefixKind.None) =>
			CreateString_Reg_SegRSI(Code.Outsb_DX_m8, addressSize, Register.DX, prefixSegment, repPrefix);

		/// <summary>
		/// Creates an outsw instruction
		/// </summary>
		/// <param name="addressSize">16, 32, or 64</param>
		/// <param name="prefixSegment">Segment override or <see cref="Register.None"/></param>
		/// <param name="repPrefix">Rep prefix</param>
		/// <returns></returns>
		public static Instruction CreateOutsw(int addressSize, Register prefixSegment = Register.None, RepPrefixKind repPrefix = RepPrefixKind.None) =>
			CreateString_Reg_SegRSI(Code.Outsw_DX_m16, addressSize, Register.DX, prefixSegment, repPrefix);

		/// <summary>
		/// Creates an outsd instruction
		/// </summary>
		/// <param name="addressSize">16, 32, or 64</param>
		/// <param name="prefixSegment">Segment override or <see cref="Register.None"/></param>
		/// <param name="repPrefix">Rep prefix</param>
		/// <returns></returns>
		public static Instruction CreateOutsd(int addressSize, Register prefixSegment = Register.None, RepPrefixKind repPrefix = RepPrefixKind.None) =>
			CreateString_Reg_SegRSI(Code.Outsd_DX_m32, addressSize, Register.DX, prefixSegment, repPrefix);

		/// <summary>
		/// Creates a lodsb instruction
		/// </summary>
		/// <param name="addressSize">16, 32, or 64</param>
		/// <param name="prefixSegment">Segment override or <see cref="Register.None"/></param>
		/// <param name="repPrefix">Rep prefix</param>
		/// <returns></returns>
		public static Instruction CreateLodsb(int addressSize, Register prefixSegment = Register.None, RepPrefixKind repPrefix = RepPrefixKind.None) =>
			CreateString_Reg_SegRSI(Code.Lodsb_AL_m8, addressSize, Register.AL, prefixSegment, repPrefix);

		/// <summary>
		/// Creates a lodsw instruction
		/// </summary>
		/// <param name="addressSize">16, 32, or 64</param>
		/// <param name="prefixSegment">Segment override or <see cref="Register.None"/></param>
		/// <param name="repPrefix">Rep prefix</param>
		/// <returns></returns>
		public static Instruction CreateLodsw(int addressSize, Register prefixSegment = Register.None, RepPrefixKind repPrefix = RepPrefixKind.None) =>
			CreateString_Reg_SegRSI(Code.Lodsw_AX_m16, addressSize, Register.AX, prefixSegment, repPrefix);

		/// <summary>
		/// Creates a lodsd instruction
		/// </summary>
		/// <param name="addressSize">16, 32, or 64</param>
		/// <param name="prefixSegment">Segment override or <see cref="Register.None"/></param>
		/// <param name="repPrefix">Rep prefix</param>
		/// <returns></returns>
		public static Instruction CreateLodsd(int addressSize, Register prefixSegment = Register.None, RepPrefixKind repPrefix = RepPrefixKind.None) =>
			CreateString_Reg_SegRSI(Code.Lodsd_EAX_m32, addressSize, Register.EAX, prefixSegment, repPrefix);

		/// <summary>
		/// Creates a lodsq instruction
		/// </summary>
		/// <param name="addressSize">16, 32, or 64</param>
		/// <param name="prefixSegment">Segment override or <see cref="Register.None"/></param>
		/// <param name="repPrefix">Rep prefix</param>
		/// <returns></returns>
		public static Instruction CreateLodsq(int addressSize, Register prefixSegment = Register.None, RepPrefixKind repPrefix = RepPrefixKind.None) =>
			CreateString_Reg_SegRSI(Code.Lodsq_RAX_m64, addressSize, Register.RAX, prefixSegment, repPrefix);

		static Instruction CreateString_Reg_SegRSI(Code code, int addressSize, Register register, Register prefixSegment, RepPrefixKind repPrefix) {
			Instruction instruction = default;
			instruction.Code = code;

			if (repPrefix == RepPrefixKind.Repe)
				instruction.InternalSetHasRepePrefix();
			else if (repPrefix == RepPrefixKind.Repne)
				instruction.InternalSetHasRepnePrefix();
			else
				Debug.Assert(repPrefix == RepPrefixKind.None);

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp0Kind = OpKind.Register;
			instruction.InternalOp0Register = register;

			if (addressSize == 64)
				instruction.InternalOp1Kind = OpKind.MemorySegRSI;
			else if (addressSize == 32)
				instruction.InternalOp1Kind = OpKind.MemorySegESI;
			else if (addressSize == 16)
				instruction.InternalOp1Kind = OpKind.MemorySegSI;
			else
				throw new ArgumentOutOfRangeException(nameof(addressSize));

			instruction.SegmentPrefix = prefixSegment;

			Debug.Assert(instruction.OpCount == 2);
			return instruction;
		}

		/// <summary>
		/// Creates a scasb instruction
		/// </summary>
		/// <param name="addressSize">16, 32, or 64</param>
		/// <param name="repPrefix">Rep prefix</param>
		/// <returns></returns>
		public static Instruction CreateScasb(int addressSize, RepPrefixKind repPrefix = RepPrefixKind.None) =>
			CreateString_Reg_ESRDI(Code.Scasb_AL_m8, addressSize, Register.AL, repPrefix);

		/// <summary>
		/// Creates a scasw instruction
		/// </summary>
		/// <param name="addressSize">16, 32, or 64</param>
		/// <param name="repPrefix">Rep prefix</param>
		/// <returns></returns>
		public static Instruction CreateScasw(int addressSize, RepPrefixKind repPrefix = RepPrefixKind.None) =>
			CreateString_Reg_ESRDI(Code.Scasw_AX_m16, addressSize, Register.AX, repPrefix);

		/// <summary>
		/// Creates a scasd instruction
		/// </summary>
		/// <param name="addressSize">16, 32, or 64</param>
		/// <param name="repPrefix">Rep prefix</param>
		/// <returns></returns>
		public static Instruction CreateScasd(int addressSize, RepPrefixKind repPrefix = RepPrefixKind.None) =>
			CreateString_Reg_ESRDI(Code.Scasd_EAX_m32, addressSize, Register.EAX, repPrefix);

		/// <summary>
		/// Creates a scasq instruction
		/// </summary>
		/// <param name="addressSize">16, 32, or 64</param>
		/// <param name="repPrefix">Rep prefix</param>
		/// <returns></returns>
		public static Instruction CreateScasq(int addressSize, RepPrefixKind repPrefix = RepPrefixKind.None) =>
			CreateString_Reg_ESRDI(Code.Scasq_RAX_m64, addressSize, Register.RAX, repPrefix);

		static Instruction CreateString_Reg_ESRDI(Code code, int addressSize, Register register, RepPrefixKind repPrefix) {
			Instruction instruction = default;
			instruction.Code = code;

			if (repPrefix == RepPrefixKind.Repe)
				instruction.InternalSetHasRepePrefix();
			else if (repPrefix == RepPrefixKind.Repne)
				instruction.InternalSetHasRepnePrefix();
			else
				Debug.Assert(repPrefix == RepPrefixKind.None);

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp0Kind = OpKind.Register;
			instruction.InternalOp0Register = register;

			if (addressSize == 64)
				instruction.InternalOp1Kind = OpKind.MemoryESRDI;
			else if (addressSize == 32)
				instruction.InternalOp1Kind = OpKind.MemoryESEDI;
			else if (addressSize == 16)
				instruction.InternalOp1Kind = OpKind.MemoryESDI;
			else
				throw new ArgumentOutOfRangeException(nameof(addressSize));

			Debug.Assert(instruction.OpCount == 2);
			return instruction;
		}

		/// <summary>
		/// Creates a insb instruction
		/// </summary>
		/// <param name="addressSize">16, 32, or 64</param>
		/// <param name="repPrefix">Rep prefix</param>
		/// <returns></returns>
		public static Instruction CreateInsb(int addressSize, RepPrefixKind repPrefix = RepPrefixKind.None) =>
			CreateString_ESRDI_Reg(Code.Insb_m8_DX, addressSize, Register.DX, repPrefix);

		/// <summary>
		/// Creates a insw instruction
		/// </summary>
		/// <param name="addressSize">16, 32, or 64</param>
		/// <param name="repPrefix">Rep prefix</param>
		/// <returns></returns>
		public static Instruction CreateInsw(int addressSize, RepPrefixKind repPrefix = RepPrefixKind.None) =>
			CreateString_ESRDI_Reg(Code.Insw_m16_DX, addressSize, Register.DX, repPrefix);

		/// <summary>
		/// Creates a insd instruction
		/// </summary>
		/// <param name="addressSize">16, 32, or 64</param>
		/// <param name="repPrefix">Rep prefix</param>
		/// <returns></returns>
		public static Instruction CreateInsd(int addressSize, RepPrefixKind repPrefix = RepPrefixKind.None) =>
			CreateString_ESRDI_Reg(Code.Insd_m32_DX, addressSize, Register.DX, repPrefix);

		/// <summary>
		/// Creates a stosb instruction
		/// </summary>
		/// <param name="addressSize">16, 32, or 64</param>
		/// <param name="repPrefix">Rep prefix</param>
		/// <returns></returns>
		public static Instruction CreateStosb(int addressSize, RepPrefixKind repPrefix = RepPrefixKind.None) =>
			CreateString_ESRDI_Reg(Code.Stosb_m8_AL, addressSize, Register.AL, repPrefix);

		/// <summary>
		/// Creates a stosw instruction
		/// </summary>
		/// <param name="addressSize">16, 32, or 64</param>
		/// <param name="repPrefix">Rep prefix</param>
		/// <returns></returns>
		public static Instruction CreateStosw(int addressSize, RepPrefixKind repPrefix = RepPrefixKind.None) =>
			CreateString_ESRDI_Reg(Code.Stosw_m16_AX, addressSize, Register.AX, repPrefix);

		/// <summary>
		/// Creates a stosd instruction
		/// </summary>
		/// <param name="addressSize">16, 32, or 64</param>
		/// <param name="repPrefix">Rep prefix</param>
		/// <returns></returns>
		public static Instruction CreateStosd(int addressSize, RepPrefixKind repPrefix = RepPrefixKind.None) =>
			CreateString_ESRDI_Reg(Code.Stosd_m32_EAX, addressSize, Register.EAX, repPrefix);

		/// <summary>
		/// Creates a stosq instruction
		/// </summary>
		/// <param name="addressSize">16, 32, or 64</param>
		/// <param name="repPrefix">Rep prefix</param>
		/// <returns></returns>
		public static Instruction CreateStosq(int addressSize, RepPrefixKind repPrefix = RepPrefixKind.None) =>
			CreateString_ESRDI_Reg(Code.Stosq_m64_RAX, addressSize, Register.RAX, repPrefix);

		static Instruction CreateString_ESRDI_Reg(Code code, int addressSize, Register register, RepPrefixKind repPrefix) {
			Instruction instruction = default;
			instruction.Code = code;

			if (repPrefix == RepPrefixKind.Repe)
				instruction.InternalSetHasRepePrefix();
			else if (repPrefix == RepPrefixKind.Repne)
				instruction.InternalSetHasRepnePrefix();
			else
				Debug.Assert(repPrefix == RepPrefixKind.None);

			if (addressSize == 64)
				instruction.InternalOp0Kind = OpKind.MemoryESRDI;
			else if (addressSize == 32)
				instruction.InternalOp0Kind = OpKind.MemoryESEDI;
			else if (addressSize == 16)
				instruction.InternalOp0Kind = OpKind.MemoryESDI;
			else
				throw new ArgumentOutOfRangeException(nameof(addressSize));

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp1Kind = OpKind.Register;
			instruction.InternalOp1Register = register;

			Debug.Assert(instruction.OpCount == 2);
			return instruction;
		}

		/// <summary>
		/// Creates a cmpsb instruction
		/// </summary>
		/// <param name="addressSize">16, 32, or 64</param>
		/// <param name="prefixSegment">Segment override or <see cref="Register.None"/></param>
		/// <param name="repPrefix">Rep prefix</param>
		/// <returns></returns>
		public static Instruction CreateCmpsb(int addressSize, Register prefixSegment = Register.None, RepPrefixKind repPrefix = RepPrefixKind.None) =>
			CreateString_SegRSI_ESRDI(Code.Cmpsb_m8_m8, addressSize, prefixSegment, repPrefix);

		/// <summary>
		/// Creates a cmpsw instruction
		/// </summary>
		/// <param name="addressSize">16, 32, or 64</param>
		/// <param name="prefixSegment">Segment override or <see cref="Register.None"/></param>
		/// <param name="repPrefix">Rep prefix</param>
		/// <returns></returns>
		public static Instruction CreateCmpsw(int addressSize, Register prefixSegment = Register.None, RepPrefixKind repPrefix = RepPrefixKind.None) =>
			CreateString_SegRSI_ESRDI(Code.Cmpsw_m16_m16, addressSize, prefixSegment, repPrefix);

		/// <summary>
		/// Creates a cmpsd instruction
		/// </summary>
		/// <param name="addressSize">16, 32, or 64</param>
		/// <param name="prefixSegment">Segment override or <see cref="Register.None"/></param>
		/// <param name="repPrefix">Rep prefix</param>
		/// <returns></returns>
		public static Instruction CreateCmpsd(int addressSize, Register prefixSegment = Register.None, RepPrefixKind repPrefix = RepPrefixKind.None) =>
			CreateString_SegRSI_ESRDI(Code.Cmpsd_m32_m32, addressSize, prefixSegment, repPrefix);

		/// <summary>
		/// Creates a cmpsq instruction
		/// </summary>
		/// <param name="addressSize">16, 32, or 64</param>
		/// <param name="prefixSegment">Segment override or <see cref="Register.None"/></param>
		/// <param name="repPrefix">Rep prefix</param>
		/// <returns></returns>
		public static Instruction CreateCmpsq(int addressSize, Register prefixSegment = Register.None, RepPrefixKind repPrefix = RepPrefixKind.None) =>
			CreateString_SegRSI_ESRDI(Code.Cmpsq_m64_m64, addressSize, prefixSegment, repPrefix);

		static Instruction CreateString_SegRSI_ESRDI(Code code, int addressSize, Register prefixSegment, RepPrefixKind repPrefix) {
			Instruction instruction = default;
			instruction.Code = code;

			if (repPrefix == RepPrefixKind.Repe)
				instruction.InternalSetHasRepePrefix();
			else if (repPrefix == RepPrefixKind.Repne)
				instruction.InternalSetHasRepnePrefix();
			else
				Debug.Assert(repPrefix == RepPrefixKind.None);

			if (addressSize == 64) {
				instruction.InternalOp0Kind = OpKind.MemorySegRSI;
				instruction.InternalOp1Kind = OpKind.MemoryESRDI;
			}
			else if (addressSize == 32) {
				instruction.InternalOp0Kind = OpKind.MemorySegESI;
				instruction.InternalOp1Kind = OpKind.MemoryESEDI;
			}
			else if (addressSize == 16) {
				instruction.InternalOp0Kind = OpKind.MemorySegSI;
				instruction.InternalOp1Kind = OpKind.MemoryESDI;
			}
			else
				throw new ArgumentOutOfRangeException(nameof(addressSize));

			instruction.SegmentPrefix = prefixSegment;

			Debug.Assert(instruction.OpCount == 2);
			return instruction;
		}

		/// <summary>
		/// Creates a movsb instruction
		/// </summary>
		/// <param name="addressSize">16, 32, or 64</param>
		/// <param name="prefixSegment">Segment override or <see cref="Register.None"/></param>
		/// <param name="repPrefix">Rep prefix</param>
		/// <returns></returns>
		public static Instruction CreateMovsb(int addressSize, Register prefixSegment = Register.None, RepPrefixKind repPrefix = RepPrefixKind.None) =>
			CreateString_ESRDI_SegRSI(Code.Movsb_m8_m8, addressSize, prefixSegment, repPrefix);

		/// <summary>
		/// Creates a movsw instruction
		/// </summary>
		/// <param name="addressSize">16, 32, or 64</param>
		/// <param name="prefixSegment">Segment override or <see cref="Register.None"/></param>
		/// <param name="repPrefix">Rep prefix</param>
		/// <returns></returns>
		public static Instruction CreateMovsw(int addressSize, Register prefixSegment = Register.None, RepPrefixKind repPrefix = RepPrefixKind.None) =>
			CreateString_ESRDI_SegRSI(Code.Movsw_m16_m16, addressSize, prefixSegment, repPrefix);

		/// <summary>
		/// Creates a movsd instruction
		/// </summary>
		/// <param name="addressSize">16, 32, or 64</param>
		/// <param name="prefixSegment">Segment override or <see cref="Register.None"/></param>
		/// <param name="repPrefix">Rep prefix</param>
		/// <returns></returns>
		public static Instruction CreateMovsd(int addressSize, Register prefixSegment = Register.None, RepPrefixKind repPrefix = RepPrefixKind.None) =>
			CreateString_ESRDI_SegRSI(Code.Movsd_m32_m32, addressSize, prefixSegment, repPrefix);

		/// <summary>
		/// Creates a movsq instruction
		/// </summary>
		/// <param name="addressSize">16, 32, or 64</param>
		/// <param name="prefixSegment">Segment override or <see cref="Register.None"/></param>
		/// <param name="repPrefix">Rep prefix</param>
		/// <returns></returns>
		public static Instruction CreateMovsq(int addressSize, Register prefixSegment = Register.None, RepPrefixKind repPrefix = RepPrefixKind.None) =>
			CreateString_ESRDI_SegRSI(Code.Movsq_m64_m64, addressSize, prefixSegment, repPrefix);

		static Instruction CreateString_ESRDI_SegRSI(Code code, int addressSize, Register prefixSegment, RepPrefixKind repPrefix) {
			Instruction instruction = default;
			instruction.Code = code;

			if (repPrefix == RepPrefixKind.Repe)
				instruction.InternalSetHasRepePrefix();
			else if (repPrefix == RepPrefixKind.Repne)
				instruction.InternalSetHasRepnePrefix();
			else
				Debug.Assert(repPrefix == RepPrefixKind.None);

			if (addressSize == 64) {
				instruction.InternalOp0Kind = OpKind.MemoryESRDI;
				instruction.InternalOp1Kind = OpKind.MemorySegRSI;
			}
			else if (addressSize == 32) {
				instruction.InternalOp0Kind = OpKind.MemoryESEDI;
				instruction.InternalOp1Kind = OpKind.MemorySegESI;
			}
			else if (addressSize == 16) {
				instruction.InternalOp0Kind = OpKind.MemoryESDI;
				instruction.InternalOp1Kind = OpKind.MemorySegSI;
			}
			else
				throw new ArgumentOutOfRangeException(nameof(addressSize));

			instruction.SegmentPrefix = prefixSegment;

			Debug.Assert(instruction.OpCount == 2);
			return instruction;
		}

		/// <summary>
		/// Creates a maskmovq instruction
		/// </summary>
		/// <param name="addressSize">16, 32, or 64</param>
		/// <param name="register1">Register</param>
		/// <param name="register2">Register</param>
		/// <param name="prefixSegment">Segment override or <see cref="Register.None"/></param>
		/// <returns></returns>
		public static Instruction CreateMaskmovq(int addressSize, Register register1, Register register2, Register prefixSegment = Register.None) =>
			CreateMaskmov(Code.Maskmovq_rDI_mm_mm, addressSize, register1, register2, prefixSegment);

		/// <summary>
		/// Creates a maskmovdqu instruction
		/// </summary>
		/// <param name="addressSize">16, 32, or 64</param>
		/// <param name="register1">Register</param>
		/// <param name="register2">Register</param>
		/// <param name="prefixSegment">Segment override or <see cref="Register.None"/></param>
		/// <returns></returns>
		public static Instruction CreateMaskmovdqu(int addressSize, Register register1, Register register2, Register prefixSegment = Register.None) =>
			CreateMaskmov(Code.Maskmovdqu_rDI_xmm_xmm, addressSize, register1, register2, prefixSegment);

		/// <summary>
		/// Creates a vmaskmovdqu instruction
		/// </summary>
		/// <param name="addressSize">16, 32, or 64</param>
		/// <param name="register1">Register</param>
		/// <param name="register2">Register</param>
		/// <param name="prefixSegment">Segment override or <see cref="Register.None"/></param>
		/// <returns></returns>
		public static Instruction CreateVmaskmovdqu(int addressSize, Register register1, Register register2, Register prefixSegment = Register.None) =>
			CreateMaskmov(Code.VEX_Vmaskmovdqu_rDI_xmm_xmm, addressSize, register1, register2, prefixSegment);

		static Instruction CreateMaskmov(Code code, int addressSize, Register register1, Register register2, Register prefixSegment) {
			Instruction instruction = default;
			instruction.Code = code;

			if (addressSize == 64)
				instruction.InternalOp0Kind = OpKind.MemorySegRDI;
			else if (addressSize == 32)
				instruction.InternalOp0Kind = OpKind.MemorySegEDI;
			else if (addressSize == 16)
				instruction.InternalOp0Kind = OpKind.MemorySegDI;
			else
				throw new ArgumentOutOfRangeException(nameof(addressSize));

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp1Kind = OpKind.Register;
			instruction.InternalOp1Register = register1;

			Debug.Assert(OpKind.Register == 0);
			//instruction.InternalOp2Kind = OpKind.Register;
			instruction.InternalOp2Register = register2;

			instruction.SegmentPrefix = prefixSegment;

			Debug.Assert(instruction.OpCount == 3);
			return instruction;
		}
	}

	/// <summary>
	/// rep/repe/repne prefix
	/// </summary>
	public enum RepPrefixKind {
		/// <summary>
		/// No rep/repe/repne prefix
		/// </summary>
		None,

		/// <summary>
		/// repe prefix
		/// </summary>
		Repe,

		/// <summary>
		/// repne prefix
		/// </summary>
		Repne,

		/// <summary>
		/// rep prefix
		/// </summary>
		Rep = Repe,
	}
}
#endif
