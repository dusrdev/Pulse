use std::{path::Path, process::{Command, Stdio, ExitCode}};

fn main() -> ExitCode {
    let commands: Vec<&str> = vec![
        "--help",
        "get-sample",
        "sample.json -u \"http://127.0.0.1:3000/\"", // one string
        "sample.json -n 100 -u \"http://127.0.0.1:3000/json\" --json", // 100 json with formatting
        "sample.json -n 100 -c 10 -u \"http://127.0.0.1:3000/html\" --raw", // 100 html limited raw
        "sample.json -n 10 -u \"http://127.0.0.1:3000/html\" --raw -f", // 10 html full equality
        "sample.json -n 100 -v -u \"http://127.0.0.1:3000/html\" --raw", // 100 html parallel verbose (race) raw
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
    let dir = &Path::new("../../../src/Pulse/publish/");
    let path = &Path::new("../../../src/Pulse/publish/Pulse");

    let mut binding = Command::new(path);

    let command = binding
        .current_dir(&dir)
        .arg(&args)
        .stdin(Stdio::null())
        .stdout(Stdio::null())
        .stderr(Stdio::piped());

    let output = command.output().unwrap();

    if !output.status.success() {
        // preserve stderr in the error
        let code = output.status.code().unwrap();
        let err = String::from_utf8_lossy(&output.stderr);
        println!("Command failed with code: {code}");
        println!("Message:");
        println!("{err}");
        return code;
    }

    return 0;
}