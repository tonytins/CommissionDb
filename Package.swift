// swift-tools-version: 6.3

import PackageDescription

let package = Package(
    name: "Commis",
    products: [
        .executable(name: "commis", targets: ["Commis"]),
    ],
    dependencies: [
        .package(url: "https://github.com/apple/swift-argument-parser.git", from: "1.2.0"),
        .package(url: "https://github.com/Lighter-swift/Lighter.git", from: "1.4.0"),
        .package(url: "https://github.com/tonytins/GitVersion.git", from: "0.3.0"),
    ],
    targets: [
        .executableTarget(
            name: "Commis",
            dependencies: [
                .product(name: "ArgumentParser", package: "swift-argument-parser"),
                .product(name: "Lighter", package: "Lighter"),
                .product(name: "GitVersion", package: "GitVersion"),
            ],
            plugins: [
                .plugin(name: "Enlighter", package: "Lighter"),
                .plugin(name: "GitVersionPlugin", package: "GitVersion"),
            ]
        ),
        .testTarget(
            name: "CommisTests",
            dependencies: ["Commis"]
        ),
    ],
    swiftLanguageModes: [.v6]
)
