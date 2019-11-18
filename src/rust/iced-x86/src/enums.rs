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

#![allow(dead_code)] //TODO: REMOVE

// GENERATOR-BEGIN: CodeSize
// ⚠️This was generated by GENERATOR!🦹‍♂️
/// The code size (16/32/64) that was used when an instruction was decoded
#[derive(Copy, Clone, Eq, PartialEq)]
pub enum CodeSize {
	/// Unknown size
	Unknown,
	/// 16-bit code
	Code16,
	/// 32-bit code
	Code32,
	/// 64-bit code
	Code64,
}
// GENERATOR-END: CodeSize

// GENERATOR-BEGIN: RoundingControl
// ⚠️This was generated by GENERATOR!🦹‍♂️
/// Rounding control
#[derive(Copy, Clone, Eq, PartialEq)]
pub enum RoundingControl {
	/// No rounding mode
	None = 0,
	/// Round to nearest (even)
	RoundToNearest = 1,
	/// Round down (toward -inf)
	RoundDown = 2,
	/// Round up (toward +inf)
	RoundUp = 3,
	/// Round toward zero (truncate)
	RoundTowardZero = 4,
}
// GENERATOR-END: RoundingControl

#[allow(non_camel_case_types)] // Needed by rustc < 1.25.0
// GENERATOR-BEGIN: OpKind
// ⚠️This was generated by GENERATOR!🦹‍♂️
/// Instruction operand kind
#[derive(Copy, Clone, Eq, PartialEq, Ord, PartialOrd, Hash)]
pub enum OpKind {
	/// A register (`Register`).
	///
	/// This operand kind uses `Instruction::op0_register()`, `Instruction::op1_register()`, `Instruction::op2_register()`, `Instruction::op3_register()` or `Instruction::op4_register()` depending on operand number. See also `Instruction::op_register()`.
	Register,
	/// Near 16-bit branch. This operand kind uses `Instruction::near_branch16()`
	NearBranch16,
	/// Near 32-bit branch. This operand kind uses `Instruction::near_branch32()`
	NearBranch32,
	/// Near 64-bit branch. This operand kind uses `Instruction::near_branch64()`
	NearBranch64,
	/// Far 16-bit branch. This operand kind uses `Instruction::far_branch16()` and `Instruction::far_branch_selector()`
	FarBranch16,
	/// Far 32-bit branch. This operand kind uses `Instruction::far_branch32()` and `Instruction::far_branch_selector()`
	FarBranch32,
	/// 8-bit constant. This operand kind uses `Instruction::immediate8()`
	Immediate8,
	/// 8-bit constant used by the `enter`, `extrq`, `insertq` instructions. This operand kind uses `Instruction::immediate8_2nd()`
	Immediate8_2nd,
	/// 16-bit constant. This operand kind uses `Instruction::immediate16()`
	Immediate16,
	/// 32-bit constant. This operand kind uses `Instruction::immediate32()`
	Immediate32,
	/// 64-bit constant. This operand kind uses `Instruction::immediate64()`
	Immediate64,
	/// An 8-bit value sign extended to 16 bits. This operand kind uses `Instruction::immediate8to16()`
	Immediate8to16,
	/// An 8-bit value sign extended to 32 bits. This operand kind uses `Instruction::immediate8to32()`
	Immediate8to32,
	/// An 8-bit value sign extended to 64 bits. This operand kind uses `Instruction::immediate8to64()`
	Immediate8to64,
	/// A 32-bit value sign extended to 64 bits. This operand kind uses `Instruction::immediate32to64()`
	Immediate32to64,
	/// `seg:[si]`. This operand kind uses `Instruction::memory_size()`, `Instruction::memory_segment()`, `Instruction::segment_prefix()`
	MemorySegSI,
	/// `seg:[esi]`. This operand kind uses `Instruction::memory_size()`, `Instruction::memory_segment()`, `Instruction::segment_prefix()`
	MemorySegESI,
	/// `seg:[rsi]`. This operand kind uses `Instruction::memory_size()`, `Instruction::memory_segment()`, `Instruction::segment_prefix()`
	MemorySegRSI,
	/// `seg:[di]`. This operand kind uses `Instruction::memory_size()`, `Instruction::memory_segment()`, `Instruction::segment_prefix()`
	MemorySegDI,
	/// `seg:[edi]`. This operand kind uses `Instruction::memory_size()`, `Instruction::memory_segment()`, `Instruction::segment_prefix()`
	MemorySegEDI,
	/// `seg:[rdi]`. This operand kind uses `Instruction::memory_size()`, `Instruction::memory_segment()`, `Instruction::segment_prefix()`
	MemorySegRDI,
	/// `es:[di]`. This operand kind uses `Instruction::memory_size()`
	MemoryESDI,
	/// `es:[edi]`. This operand kind uses `Instruction::memory_size()`
	MemoryESEDI,
	/// `es:[rdi]`. This operand kind uses `Instruction::memory_size()`
	MemoryESRDI,
	/// 64-bit offset `[xxxxxxxxxxxxxxxx]`. This operand kind uses `Instruction::memory_address64()`, `Instruction::memory_segment()`, `Instruction::segment_prefix()`, `Instruction::memory_size()`
	Memory64,
	/// Memory operand.
	///
	/// This operand kind uses `Instruction::memory_displ_size()`, `Instruction::memory_size()`, `Instruction::memory_index_scale()`, `Instruction::memory_displacement()`, `Instruction::memory_base()`, `Instruction::memory_index()`, `Instruction::memory_segment()`, `Instruction::segment_prefix()`
	Memory,
}
// GENERATOR-END: OpKind

// GENERATOR-BEGIN: VectorLength
// ⚠️This was generated by GENERATOR!🦹‍♂️
#[derive(Copy, Clone, Eq, PartialEq)]
#[repr(u8)]
#[cfg(any(feature = "DECODER", feature = "ENCODER"))]
pub(crate) enum VectorLength {
	L128,
	L256,
	L512,
	Unknown,
}
// GENERATOR-END: VectorLength

impl Default for VectorLength {
	#[cfg_attr(has_must_use, must_use)]
	#[inline]
	fn default() -> Self {
		VectorLength::L128
	}
}

// GENERATOR-BEGIN: MandatoryPrefixByte
// ⚠️This was generated by GENERATOR!🦹‍♂️
#[derive(Copy, Clone, Eq, PartialEq)]
#[cfg(any(feature = "DECODER", feature = "ENCODER"))]
pub(crate) enum MandatoryPrefixByte {
	None,
	P66,
	PF3,
	PF2,
}
// GENERATOR-END: MandatoryPrefixByte

impl Default for MandatoryPrefixByte {
	#[cfg_attr(has_must_use, must_use)]
	#[inline]
	fn default() -> Self {
		MandatoryPrefixByte::None
	}
}

// GENERATOR-BEGIN: EncodingKind
// ⚠️This was generated by GENERATOR!🦹‍♂️
/// Instruction encoding
#[derive(Copy, Clone, Eq, PartialEq)]
#[cfg(any(feature = "DECODER", feature = "ENCODER", feature = "INSTR_INFO"))]
pub enum EncodingKind {
	/// Legacy encoding
	Legacy,
	/// VEX encoding
	VEX,
	/// EVEX encoding
	EVEX,
	/// XOP encoding
	XOP,
	/// 3DNow! encoding
	D3NOW,
}
// GENERATOR-END: EncodingKind

// GENERATOR-BEGIN: TupleType
// ⚠️This was generated by GENERATOR!🦹‍♂️
/// Tuple type (EVEX) which can be used to get the disp8 scale factor `N`
#[derive(Copy, Clone, Eq, PartialEq, Ord, PartialOrd, Hash)]
#[cfg(any(feature = "DECODER", feature = "ENCODER"))]
#[allow(non_camel_case_types)]
pub enum TupleType {
	/// `N = 1`
	None,
	/// `N = b ? (W ? 8 : 4) : 16`
	Full_128,
	/// `N = b ? (W ? 8 : 4) : 32`
	Full_256,
	/// `N = b ? (W ? 8 : 4) : 64`
	Full_512,
	/// `N = b ? 4 : 8`
	Half_128,
	/// `N = b ? 4 : 16`
	Half_256,
	/// `N = b ? 4 : 32`
	Half_512,
	/// `N = 16`
	Full_Mem_128,
	/// `N = 32`
	Full_Mem_256,
	/// `N = 64`
	Full_Mem_512,
	/// `N = W ? 8 : 4`
	Tuple1_Scalar,
	/// `N = 1`
	Tuple1_Scalar_1,
	/// `N = 2`
	Tuple1_Scalar_2,
	/// `N = 4`
	Tuple1_Scalar_4,
	/// `N = 8`
	Tuple1_Scalar_8,
	/// `N = 4`
	Tuple1_Fixed_4,
	/// `N = 8`
	Tuple1_Fixed_8,
	/// `N = W ? 16 : 8`
	Tuple2,
	/// `N = W ? 32 : 16`
	Tuple4,
	/// `N = W ? error : 32`
	Tuple8,
	/// `N = 16`
	Tuple1_4X,
	/// `N = 8`
	Half_Mem_128,
	/// `N = 16`
	Half_Mem_256,
	/// `N = 32`
	Half_Mem_512,
	/// `N = 4`
	Quarter_Mem_128,
	/// `N = 8`
	Quarter_Mem_256,
	/// `N = 16`
	Quarter_Mem_512,
	/// `N = 2`
	Eighth_Mem_128,
	/// `N = 4`
	Eighth_Mem_256,
	/// `N = 8`
	Eighth_Mem_512,
	/// `N = 16`
	Mem128,
	/// `N = 8`
	MOVDDUP_128,
	/// `N = 32`
	MOVDDUP_256,
	/// `N = 64`
	MOVDDUP_512,
}
// GENERATOR-END: TupleType
