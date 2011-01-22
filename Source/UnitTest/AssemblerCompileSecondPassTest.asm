
macro r0 { 0 }
macro r1 { 1 }
macro r7 { 7 }

macro a0 { 0 }
macro a1 { 1 }
macro a3 { 3 }

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
	value & 0xFF
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

macro test a, b, c {
	lr a
	m b, c
	r0 + r1 * a1
}

macro main {
	error "hello, world!"
}

