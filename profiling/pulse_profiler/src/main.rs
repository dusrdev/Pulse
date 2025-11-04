use std::{path::Path, process::{Command, Stdio, ExitCode}};

fn main() -> ExitCode {
    let commands: Vec<&str> = vec![
        "--help",
        "get-sample",
        ""
    ];

    for command in &commands {
        let code = run_pulse(&command);

        if code != 0 {
            return ExitCode::from(1);
        }
    }

    return ExitCode::from(0);
}

fn run_pulse(args: &str) -> i32 {
    let path = &Path::new("../../../src/Pulse/publish/Pulse");

    let mut binding = Command::new(path);

    let command = binding
        .arg(&args)
        .stdin(Stdio::null())
        .stdout(Stdio::null())
        .stderr(Stdio::piped());

    let output = command.output().unwrap();

    if !output.status.success() {
        // preserve stderr in the error
        let code = output.status.code().unwrap();
        let err = String::from_utf8_lossy(&output.stderr);
        println!("Command failed with code: ${code}");
        println!("Message:");
        println!("${err}");
        return code;
    }

    return 0;
}