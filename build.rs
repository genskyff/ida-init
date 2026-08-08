fn main() {
    println!("cargo:rerun-if-changed=assets/icon.ico");

    if !matches!(
        std::env::var("CARGO_CFG_TARGET_OS").as_deref(),
        Ok("windows")
    ) {
        return;
    }

    let mut resource = winresource::WindowsResource::new();
    resource.set_icon("assets/icon.ico");
    resource
        .compile()
        .expect("failed to compile Windows resources");
}
