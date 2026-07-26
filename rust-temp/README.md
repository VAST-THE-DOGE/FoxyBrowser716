# rust-temp

Throwaway spike. Not the Rust rewrite.

This directory exists to answer the questions in [`Docs/todo/rust-poc.md`](../Docs/todo/rust-poc.md):
can CEF render offscreen into a texture Slint composites, can UI draw over live web content, and do
extensions work under Chrome-style OSR. It is scaffolding for producing findings.

**Nothing here graduates by copy-paste.** When the PoC has answered its questions, the code moves to
its real home as a design review: every piece gets asked whether it should exist in that shape at
all. Module layout, CEF lifetime handling, and input plumbing are all expected to be redesigned once
the constraints are known rather than guessed.

Judge this code by whether it answered the question, not by whether it is good.
