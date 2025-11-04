use axum::{
    http::HeaderName,
    response::Html, // Use this for a proper text/html content type
    routing::get,
    Router,
};
use std::net::SocketAddr;

const JSON_PAYLOAD: &str = include_str!("../data/payload.json");

const HTML_PAYLOAD: &str = include_str!("../data/payload.html");

#[tokio::main]
async fn main() {
    let app = Router::new()
        .route("/", get(root_handler))
        .route("/html", get(html_handler))
        .route("/json", get(json_handler));

    let addr = SocketAddr::from(([127, 0, 0, 1], 3000));
    println!("Test server with HTML/JSON endpoints listening on {addr}");

    let listener = tokio::net::TcpListener::bind(addr).await.unwrap();
    axum::serve(listener, app).await.unwrap();
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