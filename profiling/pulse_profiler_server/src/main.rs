use axum::{
    Router,
    http::HeaderName,
    response::Html, // Use this for a proper text/html content type
    routing::get,
};
use tokio::signal;
use std::net::SocketAddr;

const JSON_PAYLOAD: &str = include_str!("../data/payload.json");

const HTML_PAYLOAD: &str = include_str!("../data/payload.html");

#[tokio::main]
async fn main() {
    let app = Router::new()
        .route("/", get(root_handler))
        .route("/html", get(html_handler))
        .route("/json", get(json_handler));

    let addr = SocketAddr::from(([0, 0, 0, 0], 3000));
    println!("Test server with HTML/JSON endpoints listening on {addr}");

    let listener = tokio::net::TcpListener::bind(addr).await.unwrap();
    axum::serve(listener, app)
        .with_graceful_shutdown(shutdown_signal())
        .await
        .unwrap();
}

async fn root_handler() -> &'static str {
    return "Hello!";
}

async fn html_handler() -> Html<&'static str> {
    return Html(HTML_PAYLOAD);
}

async fn json_handler() -> ([(HeaderName, &'static str); 1], &'static str) {
    let headers = [(HeaderName::from_static("content-type"), "application/json")];
    return (headers, JSON_PAYLOAD);
}

// A future that completes when a shutdown signal is received.
async fn shutdown_signal() {
    // Wait for the (Ctrl+C) signal
    let ctrl_c = async {
        signal::ctrl_c()
            .await
            .expect("failed to install Ctrl+C handler");
    };
    // Wait for a termination signal (on Unix)
    #[cfg(unix)]
    let terminate = async {
        signal::unix::signal(signal::unix::SignalKind::terminate())
            .expect("failed to install signal handler")
            .recv()
            .await;
    };
    // On Windows, we only need to listen for Ctrl+C
    #[cfg(not(unix))]
    let terminate = std::future::pending::<()>();
    // Wait for either signal
    tokio::select! { _ = ctrl_c => { println!("\nCtrl+C received, shutting down...") }, _ = terminate => { println!("\nTerminate signal received, shutting down...") }, }
}
