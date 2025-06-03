(module
  (func (export "main")
    (drop
      (i32.or
        (i32.and
          (i32.eq
            (i32.add
              (i32.const 3)
              (i32.const 2)
            )
            (i32.mul
              (i32.const 5)
              (i32.const 1)
            )
          )
          (i32.ge_s
            (i32.sub
              (i32.const 10)
              (i32.const 2)
            )
            (i32.const 8)
          )
        )
        (i32.ne
          (i32.const 0)
          (i32.const 1)
        )
      )
    )
  )
)
