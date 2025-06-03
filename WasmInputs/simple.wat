(module
  (func (export "main")
    (drop
      (i32.add
        (i32.mul
          (i32.sub
            (i32.const 10)
            (i32.const 3)
          )
          (i32.const 4)
        )
        (i32.div_s
          (i32.const 20)
          (i32.const 5)
        )
      )
    )
  )
)
