
macro r0 { 0 }
macro r1 { 1 }

macro a0 { 0 }
macro a1 { 1 }

macro validateRegister r {
	if(!(r <= r0 && r <= r7)) {
		error "invalid register"
	}
}

macro validateAddress a {
	if(!(a <= a0 && a <= a3)) {
		error "invalid address register"
	}
}

macro validateByte b {
	if(!(0 <= b && b <= 0xFF)) {
		error "invalid byte"
	}
}

macro dw value {
	value & oxFF
	value >> 8
}

macro dwArray count {
	if(0 < count) {
		dw 0
		dwArray count - 1
	}
}

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; basic opeartions

; halt
macro halt {
	0
}

; load one byte data d to register r: d -> r
macro m r, d {
	validateRegister r
	validateByte d
	0b00001000 | r
	d
}

; load byte from register to r0: (r) -> r0
macro lr r {
	validateRegister r
	0b00101000 | r
}

error
