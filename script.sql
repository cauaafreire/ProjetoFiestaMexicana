DROP DATABASE IF EXISTS bdRestaurante;
CREATE DATABASE bdRestaurante;
USE bdRestaurante;

CREATE TABLE usuarios (
    id INT PRIMARY KEY AUTO_INCREMENT,
    nome VARCHAR(100),
    email VARCHAR(100) UNIQUE,
    senha_hash VARCHAR(255),
    role ENUM("Funcionario", "Admin"),
    ativo TINYINT(1) DEFAULT 1,
    criado_em DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE categoria (
    id INT PRIMARY KEY AUTO_INCREMENT,
    nome VARCHAR(150) NOT NULL,
    criado_em DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE tipo_prato (
    id INT PRIMARY KEY AUTO_INCREMENT,
    nome VARCHAR(100) NOT NULL,
    criado_em DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE origem (
    id INT PRIMARY KEY AUTO_INCREMENT,
    nome VARCHAR(150) NOT NULL,
    criado_em DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE metodo_preparo (
    id INT PRIMARY KEY AUTO_INCREMENT,
    nome VARCHAR(100) NOT NULL,
    criado_em DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE chef (
    id INT PRIMARY KEY AUTO_INCREMENT,
    nome VARCHAR(150) NOT NULL,
    criado_em DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE pratos (
    id INT PRIMARY KEY AUTO_INCREMENT,
    nome VARCHAR(200) NOT NULL,
    chef INT,
    categoria INT,
    tipo_prato INT,
    origem INT,
    metodo_preparo INT,
    preco DECIMAL(10,2) NOT NULL,
    descricao TEXT,
    disponivel TINYINT(1) DEFAULT 1,
    criado_em DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (chef) REFERENCES chef(id),
    FOREIGN KEY (categoria) REFERENCES categoria(id),
    FOREIGN KEY (tipo_prato) REFERENCES tipo_prato(id),
    FOREIGN KEY (origem) REFERENCES origem(id),
    FOREIGN KEY (metodo_preparo) REFERENCES metodo_preparo(id)
);

CREATE TABLE ingrediente (
    id INT PRIMARY KEY AUTO_INCREMENT,
    nome VARCHAR(150) NOT NULL,
    criado_em DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE prato_ingrediente (
    id_prato INT,
    id_ingrediente INT,
    quantidade VARCHAR(50),
    PRIMARY KEY (id_prato, id_ingrediente),
    FOREIGN KEY (id_prato) REFERENCES pratos(id),
    FOREIGN KEY (id_ingrediente) REFERENCES ingrediente(id)
);

CREATE TABLE pedidos (
    id INT PRIMARY KEY AUTO_INCREMENT,
    id_prato INT,
    quantidade INT,
    preco_unitario DECIMAL(10,2),
    total DECIMAL(10,2),
    status ENUM("Pendente", "Preparando", "Finalizado", "Cancelado") DEFAULT "Pendente",
    data_pedido DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (id_prato) REFERENCES pratos(id)
);

DELIMITER $$

CREATE PROCEDURE sp_usuario_criar (
    IN p_nome VARCHAR(100),
    IN p_email VARCHAR(100),
    IN p_senha_hash VARCHAR(255),
    IN p_role VARCHAR(20)
)
BEGIN
    INSERT INTO usuarios (nome, email, senha_hash, role)
    VALUES (p_nome, p_email, p_senha_hash, p_role);
END $$

CREATE PROCEDURE sp_categoria_listar()
BEGIN
    SELECT id, nome FROM categoria ORDER BY nome;
END $$

CREATE PROCEDURE sp_tipo_listar()
BEGIN
    SELECT id, nome FROM tipo_prato ORDER BY nome;
END $$

CREATE PROCEDURE sp_origem_listar()
BEGIN
    SELECT id, nome FROM origem ORDER BY nome;
END $$

CREATE PROCEDURE sp_metodo_listar()
BEGIN
    SELECT id, nome FROM metodo_preparo ORDER BY nome;
END $$

CREATE PROCEDURE sp_chef_listar()
BEGIN
    SELECT id, nome FROM chef ORDER BY nome;
END $$

CREATE PROCEDURE sp_categoria_criar(IN p_nome VARCHAR(150))
BEGIN
    INSERT INTO categoria (nome) VALUES (p_nome);
END $$

CREATE PROCEDURE sp_tipo_criar(IN p_nome VARCHAR(100))
BEGIN
    INSERT INTO tipo_prato (nome) VALUES (p_nome);
END $$

CREATE PROCEDURE sp_origem_criar(IN p_nome VARCHAR(150))
BEGIN
    INSERT INTO origem (nome) VALUES (p_nome);
END $$

CREATE PROCEDURE sp_metodo_criar(IN p_nome VARCHAR(100))
BEGIN
    INSERT INTO metodo_preparo (nome) VALUES (p_nome);
END $$

CREATE PROCEDURE sp_chef_criar(IN p_nome VARCHAR(150))
BEGIN
    INSERT INTO chef (nome) VALUES (p_nome);
END $$

CREATE PROCEDURE sp_prato_criar (
    IN p_nome VARCHAR(200),
    IN p_chef INT,
    IN p_categoria INT,
    IN p_tipo INT,
    IN p_origem INT,
    IN p_metodo INT,
    IN p_preco DECIMAL(10,2),
    IN p_descricao TEXT
)
BEGIN
    INSERT INTO pratos
    (nome, chef, categoria, tipo_prato, origem, metodo_preparo, preco, descricao)
    VALUES
    (p_nome, p_chef, p_categoria, p_tipo, p_origem, p_metodo, p_preco, p_descricao);
END $$

CREATE PROCEDURE sp_prato_listar()
BEGIN
    SELECT
        p.id,
        p.nome,
        c.nome AS chef,
        cat.nome AS categoria,
        t.nome AS tipo,
        o.nome AS origem,
        m.nome AS metodo_preparo,
        p.preco,
        p.disponivel
    FROM pratos p
    LEFT JOIN chef c ON c.id = p.chef
    LEFT JOIN categoria cat ON cat.id = p.categoria
    LEFT JOIN tipo_prato t ON t.id = p.tipo_prato
    LEFT JOIN origem o ON o.id = p.origem
    LEFT JOIN metodo_preparo m ON m.id = p.metodo_preparo
    ORDER BY p.nome;
END $$

CREATE PROCEDURE sp_prato_obter(IN p_id INT)
BEGIN
    SELECT * FROM pratos WHERE id = p_id;
END $$

CREATE PROCEDURE sp_prato_excluir(IN p_id INT)
BEGIN
    DELETE FROM pratos WHERE id = p_id;
END $$

CREATE PROCEDURE sp_pedido_criar (
    IN p_id_prato INT,
    IN p_qtd INT
)
BEGIN
    DECLARE v_preco DECIMAL(10,2);

    SELECT preco INTO v_preco FROM pratos WHERE id = p_id_prato;

    INSERT INTO pedidos (id_prato, quantidade, preco_unitario, total)
    VALUES (p_id_prato, p_qtd, v_preco, v_preco * p_qtd);
END $$

DELIMITER ;

SELECT * FROM pedidos;